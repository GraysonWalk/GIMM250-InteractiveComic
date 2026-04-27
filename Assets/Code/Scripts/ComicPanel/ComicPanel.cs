using System.Collections;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
///     A single panel in the comic sequence. Steps through IPanelStep children in hierarchy order.
///     AnimatedSteps are IPanelStep — ComicPanel doesn't distinguish between visual and minigame steps.
///     All steps are blocking: Advance() waits for each step's OnAnimationFinished event before
///     accepting the next input.
/// </summary>
public class ComicPanel : MonoBehaviour, IComicPanel
{
    #region Variables

    [SerializeField] private PanelDataSO data; // References a scriptable object used to configure the panel
    [SerializeField] private PlayerChoicesSO choices;
    [SerializeField] private CinemachineCamera cam;
    [SerializeField] private Animator anim;

    private IPanelStep[] _steps;
    private int _currentStep;
    private bool _isBlocked;
    private Coroutine _introCoroutine;

    private static readonly int IntroHash = Animator.StringToHash("Intro");

    public UnityEvent OnPanelComplete { get; } = new();

    /// <summary>
    ///     Fired whenever the panel is ready for the next advance button press:
    ///     — after the intro animation finishes (polled by coroutine)
    ///     — after a blocking step completes
    /// </summary>
    public UnityEvent OnReadyForInput { get; } = new();

    public LoopCount FirstLoop => data.FirstLoop;
    public int Rank => data.Rank;
    public CinemachineBlendDefinition IncomingBlend => data.IncomingBlend;
    public bool HasBeenVisited { get; private set; }

    #endregion

    #region Methods

    private void Awake()
    {
        if (!ValidateReferences()) return;

        // Ensure camera starts disabled regardless of how the scene was saved in the Editor.
        // ComicManager.Start() enables the first panel's camera via Show().
        // This guarantees only one CinemachineCamera is ever active at a time.
        cam.enabled = false;

        // Collect all steps from children in hierarchy order.
        _steps = GetComponentsInChildren<IPanelStep>(true);

        // Snap to time=0 of Intro so Animator-driven elements start in their hidden state
        // before Show() is called (prevents a visible flash on the first frame).
        anim.Play(IntroHash, 0, 0f);
        anim.Update(0f);
    }

    private void OnValidate()
    {
        ValidateReferences();
    }

    // Returns false and logs a clear error if any required reference is missing.
    private bool ValidateReferences()
    {
        var valid = true;
        if (data == null)
        {
            Debug.LogError($"[ComicPanel] '{gameObject.name}': PanelDataSO (data) is not assigned.", this);
            valid = false;
        }

        if (cam == null)
        {
            Debug.LogError($"[ComicPanel] '{gameObject.name}': CinemachineCamera (cam) is not assigned.", this);
            valid = false;
        }

        if (anim == null)
        {
            Debug.LogError($"[ComicPanel] '{gameObject.name}': Animator (anim) is not assigned.", this);
            valid = false;
        }

        return valid;
    }

    /// <summary>
    ///     Enables the camera, hides all steps, and resets the sequence.
    ///     Called when transitioning TO this panel with animation.
    /// </summary>
    public void Show()
    {
        if (cam == null || anim == null || _steps == null)
        {
            Debug.LogError($"[ComicPanel] '{gameObject.name}': Show() called but required references are missing. " +
                           "Check cam, anim, and data in the Inspector.", this);
            return;
        }

        _currentStep = 0;
        _isBlocked = false;
        cam.enabled = true;

        foreach (IPanelStep step in _steps)
            step.Deactivate();

        if (_introCoroutine != null) StopCoroutine(_introCoroutine);

        if (!HasBeenVisited || data.ReplayAnimationOnRevisit)
        {
            // First visit or replay — play Intro from the start and wait for it to finish.
            // Using anim.Play instead of CrossFade avoids a same-state crossfade that would
            // re-trigger any Animation Events at normalizedTime=0 immediately.
            HasBeenVisited = true;
            anim.Play(IntroHash, 0, 0f);
            _introCoroutine = StartCoroutine(WaitForIntroCompletion());
        }
        else
        {
            // Revisit with replay disabled — jump straight to the end state.
            // Defer OnReadyForInput by one frame so SwitchToPanel finishes broadcasting
            // navigation availability before the hint is shown.
            anim.Play(IntroHash, 0, 1f);
            _introCoroutine = StartCoroutine(FireReadyForInputNextFrame());
        }
    }

    /// <summary>
    ///     Enables the camera and shows only steps marked PersistsInFinalState.
    ///     Called by history navigation (UI arrows) — no animation, instant end state.
    ///     Does NOT fire OnReadyForInput; history navigation never shows the advance hint.
    /// </summary>
    public void ShowInstant()
    {
        if (_introCoroutine != null) StopCoroutine(_introCoroutine);
        _introCoroutine = null;

        cam.enabled = true;
        foreach (IPanelStep step in _steps)
            if (step.PersistsInFinalState)
                step.Activate(choices);
            else
                step.Deactivate();

        anim.Play(IntroHash, 0, 1f);
    }

    /// <summary>Disables this panel's camera and stops any running intro coroutine.</summary>
    public void Hide()
    {
        if (_introCoroutine != null) StopCoroutine(_introCoroutine);
        _introCoroutine = null;
        cam.enabled = false;
    }

    /// <summary>
    ///     Activates the next step in sequence. Locks input until the step fires OnStepComplete.
    ///     Fires OnPanelComplete when all steps have been shown.
    /// </summary>
    public void Advance()
    {
        if (_isBlocked) return;

        if (_steps.Length == 0 || _currentStep >= _steps.Length)
        {
            OnPanelComplete.Invoke();
            return;
        }

        IPanelStep step = _steps[_currentStep];
        _currentStep++;
        step.Activate(choices);

        if (step.IsBlocking)
        {
            _isBlocked = true;
            step.OnStepComplete.AddListener(UnblockPanel);
        }
        else if (_currentStep >= _steps.Length)
        {
            OnPanelComplete.Invoke();
        }
        else
        {
            OnReadyForInput.Invoke();
        }
    }

    private void UnblockPanel()
    {
        _steps[_currentStep - 1].OnStepComplete.RemoveListener(UnblockPanel);
        _isBlocked = false;

        if (_currentStep >= _steps.Length)
            OnPanelComplete.Invoke();
        else
            OnReadyForInput.Invoke();
    }

    /// <summary>
    ///     Polls the Animator each frame until the Intro animation reaches its end,
    ///     then fires OnReadyForInput. Aborts silently if the camera is disabled mid-play.
    /// </summary>
    private IEnumerator WaitForIntroCompletion()
    {
        // Wait one frame so the Animator begins processing the new Play() call,
        // and so SwitchToPanel() can finish calling BroadcastNavigationAvailability().
        yield return null;

        while (true)
        {
            if (!cam.enabled) yield break; // Panel hidden mid-animation; abort.

            AnimatorStateInfo state = anim.GetCurrentAnimatorStateInfo(0);
            if (state.shortNameHash == IntroHash && state.normalizedTime >= 1f)
                break;

            yield return null;
        }

        OnReadyForInput.Invoke();
    }

    /// <summary>
    ///     Fires OnReadyForInput after a one-frame delay.
    ///     Used when skipping to the end state (revisit, no replay) so that
    ///     BroadcastNavigationAvailability() runs first.
    /// </summary>
    private IEnumerator FireReadyForInputNextFrame()
    {
        yield return null;
        if (cam.enabled)
            OnReadyForInput.Invoke();
    }

    #endregion
}