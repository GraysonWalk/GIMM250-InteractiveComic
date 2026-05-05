using System.Collections;
using UnityEngine;

/// <summary>
///     A blocking animated step. Attach to any child GameObject of a ComicPanel.
///     Each step plays its animation when activated and locks Advance() until
///     OnAnimationFinished() is called by the Animation Event on the last keyframe.
///     Add PanelText child GameObjects to display focus-variant text alongside the animation.
///     Designer workflow:
///     1. Add a child GameObject under ComicPanel; attach this component + an Animator.
///     2. Set the Animator Controller's default state to your clip; disable Loop Time.
///     3. On the last keyframe add an Animation Event pointing to OnAnimationFinished().
///     4. Optionally: add PanelText or SpriteVariant children and fill in variant content in the Inspector.
///     5. Tick "Persists In Final State" if this element should remain visible at the panel's end.
///     If the Animation Event in step 3 is missing, a safety fallback coroutine still completes
///     the step after the clip's length elapses and logs a warning naming the offending GameObject.
/// </summary>
[RequireComponent(typeof(Animator))]
public sealed class AnimatedStep : StepBase
{
    #region Variables

    // Grace period added on top of the clip length before the safety fallback fires.
    // Gives the Animation Event a chance to land at exactly time == stopTime without racing it.
    private const float SafetyGraceSeconds = 0.25f;

    [Tooltip("If ticked, this element remains visible when the panel reaches its final state.")]
    [SerializeField] private bool persistsInFinalState;

    // True only when this step was designed to persist AND has actually been run via Advance().
    // Prevents ShowInstant() from playing the animation outside the normal blocking sequence.
    public override bool ShowInFinalState => persistsInFinalState && HasBeenActivated;

    private Animator _anim;

    // Set to true the moment OnStepComplete is fired this activation, by either the Animation Event
    // or the safety fallback. Prevents double-firing if the event lands after the fallback warning.
    private bool _completedThisActivation;

    private Coroutine _safetyCoroutine;

    #endregion

    #region Methods

    private void Awake()
    {
        _anim = GetComponent<Animator>();
    }

    /// <summary>
    ///     Called by an Animation Event on the last frame of this step's clip.
    ///     Fires OnStepComplete so ComicPanel can unblock input.
    /// </summary>
    public void OnAnimationFinished()
    {
        if (_completedThisActivation) return;
        _completedThisActivation = true;
        StopSafetyCoroutine();
        OnStepComplete.Invoke();
    }

    /// <summary>
    ///     Activates this step.
    ///     First visit (or replay): populates PanelText children and plays the animation from the start.
    ///     Revisit with hideOnRevisit = false: snaps to the final frame; no animation, no advance press.
    ///     Revisit with hideOnRevisit = true: stays hidden; no advance press. ComicPanel auto-chains.
    /// </summary>
    public override void Activate(PlayerChoicesSO choices)
    {
        bool skip = BeginActivation();

        // Reset per-activation completion state and cancel any safety coroutine left over from a
        // previous activation (replay path) before deciding which branch to take.
        _completedThisActivation = false;
        StopSafetyCoroutine();

        if (!skip)
        {
            // First visit (or replay) — populate all variant content and play the animation from the start.
            IVariantContent[] variants = GetComponentsInChildren<IVariantContent>(true);
            foreach (IVariantContent variant in variants)
                variant.Populate(choices);

            gameObject.SetActive(true);
            SeekAnimator(0f);

            // Start the safety fallback. If the clip's last-frame Animation Event is missing or
            // misconfigured, this still fires OnStepComplete after the clip length + grace period
            // so the panel never softlocks — and emits a warning identifying the offending step.
            _safetyCoroutine = StartCoroutine(SafetyFallback());
        }
        else if (!hideOnRevisit)
        {
            // Revisit, show frozen — snap to the end frame without animating.
            gameObject.SetActive(true);
            SeekAnimator(1f);
        }
        // else: hideOnRevisit = true → stay deactivated. IsBlocking is false so ComicPanel auto-chains.
    }

    public override void Deactivate()
    {
        StopSafetyCoroutine();
        base.Deactivate();
    }

    // Settles the Animator into its default state then seeks to the given normalised time.
    // Update(0f) before the hash query ensures the Entry → default-state transition is resolved.
    // GetCurrentAnimatorStateInfo is used rather than a cached hash because designers name their
    // Animator states freely — there is no single state name to cache at code-writing time.
    private void SeekAnimator(float normalizedTime)
    {
        _anim.Update(0f);
        AnimatorStateInfo state = _anim.GetCurrentAnimatorStateInfo(0);
        _anim.Play(state.fullPathHash, 0, normalizedTime);
        _anim.Update(0f);
    }

    // Watches the Animator after Activate() plays its clip and fires OnStepComplete itself if the
    // expected last-keyframe Animation Event never lands. Length is read from the Animator's
    // current state (post-seek), so it reflects whichever clip the controller actually entered.
    // SafetyGraceSeconds gives a legitimate event a chance to win the race; _completedThisActivation
    // ensures we don't double-fire if the event lands a frame after the fallback already did.
    private IEnumerator SafetyFallback()
    {
        // Wait one frame so the Animator has a chance to evaluate the new state and surface its length.
        yield return null;

        AnimatorStateInfo state = _anim.GetCurrentAnimatorStateInfo(0);
        float clipLength = state.length > 0f ? state.length : 1f;
        float deadline = Time.time + clipLength + SafetyGraceSeconds;

        while (Time.time < deadline)
        {
            if (_completedThisActivation) yield break;
            yield return null;
        }

        if (_completedThisActivation) yield break;

        Debug.LogWarning(
            $"[AnimatedStep] '{gameObject.name}' clip finished but no OnAnimationFinished animation event " +
            $"fired within {clipLength + SafetyGraceSeconds:0.00}s. Auto-completing the step. Add an " +
            "Animation Event on the last keyframe of the clip pointing to OnAnimationFinished() to silence this warning.",
            this);

        _completedThisActivation = true;
        _safetyCoroutine = null;
        OnStepComplete.Invoke();
    }

    private void StopSafetyCoroutine()
    {
        if (_safetyCoroutine == null) return;
        StopCoroutine(_safetyCoroutine);
        _safetyCoroutine = null;
    }

    /// <summary>
    ///     Called by Unity when the component is first added in the Editor.
    ///     Starts inactive and defaults replayOnRevisit to true (animated steps replay by default).
    ///     Prevents spurious TMP "No Font Asset" warnings on scene open.
    /// </summary>
    private void Reset()
    {
        gameObject.SetActive(false);
        replayOnRevisit = true;
    }

    #endregion
}