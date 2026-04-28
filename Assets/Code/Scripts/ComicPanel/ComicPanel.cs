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

        // Collect all steps (DialoguePoints, MiniGames, etc.) from children in hierarchy order.
        // Immediately deactivate them so their Animators don't tick before Show() is called —
        // mirrors the cam.enabled = false pattern above.
        _steps = GetComponentsInChildren<IPanelStep>(true);
        foreach (IPanelStep step in _steps)
            step.Deactivate();

        // Deactivate all steps so any accidentally-active child objects are hidden at scene load.
        foreach (IPanelStep step in _steps)
            step.Deactivate();

        // Snap to time=0 of Intro so Animator-driven elements start in their hidden state,
        // then disable the Animator so it doesn't keep advancing while this panel is inactive.
        // Without this, the Intro animation plays to completion in the background, leaving
        // elements fully visible from other panels' cameras before this panel is ever shown.
        anim.Play(IntroHash, 0, 0f);
        anim.Update(0f);
        anim.enabled = false;
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
        anim.enabled = true;
        cam.enabled = true;

        foreach (IPanelStep step in _steps)
            step.Deactivate();

        if (_introCoroutine != null) StopCoroutine(_introCoroutine);

        if (!HasBeenVisited || data.ReplayAnimationOnRevisit)
        {
            anim.Play(IntroHash, 0, 0f);
            anim.CrossFade(IntroHash, data.IntroCrossFadeDuration);
            _introCoroutine = StartCoroutine(WaitForIntroCompletion());
        }
        else if (!data.ReplayAnimationOnRevisit)
        {
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

        anim.enabled = true;
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
        anim.enabled = false;
    }

    /// <summary>
    ///     Replays this panel from the start, resetting any step that has replayOnRevisit = true
    ///     so it animates and blocks again. Steps with replayOnRevisit = false (e.g. FocusPoint)
    ///     are left in their completed state — they will skip without blocking on the next pass.
    ///     Called by ComicManager.ReplayCurrentPanel() when the player presses the Replay button.
    /// </summary>
    public void Replay()
    {
        if (_steps != null)
            foreach (IPanelStep step in _steps)
                step.PrepareForReplay();

        // Reset HasBeenVisited so Show() takes the first-visit path and replays the intro.
        HasBeenVisited = false;
        Show();
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
            // Last step was non-blocking. If the panel requires an explicit advance press,
            // signal ready-for-input and let the next Advance() call fire OnPanelComplete.
            // Otherwise complete immediately.
            if (data.RequireAdvanceToComplete)
                OnReadyForInput.Invoke();
            else
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
        {
            // Last step was blocking. Same choice as above — wait for explicit advance or complete now.
            if (data.RequireAdvanceToComplete)
                OnReadyForInput.Invoke();
            else
                OnPanelComplete.Invoke();
        }
        else
        {
            OnReadyForInput.Invoke();
        }
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

        // Mark as visited only now — after the animation fully completes.
        // If the player navigated away mid-animation, this line never runs,
        // so HasBeenVisited stays false and the animation replays on next visit.
        HasBeenVisited = true;
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