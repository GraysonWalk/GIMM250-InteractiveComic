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
/// </summary>
[RequireComponent(typeof(Animator))]
public sealed class AnimatedStep : StepBase
{
    #region Variables

    [Tooltip("If ticked, this element remains visible when the panel reaches its final state.")]
    [SerializeField] private bool persistsInFinalState;

    // True only when this step was designed to persist AND has actually been run via Advance().
    // Prevents ShowInstant() from playing the animation outside the normal blocking sequence.
    public override bool ShowInFinalState => persistsInFinalState && HasBeenActivated;

    private Animator _anim;

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

        if (!skip)
        {
            // First visit (or replay) — populate all variant content and play the animation from the start.
            IVariantContent[] variants = GetComponentsInChildren<IVariantContent>(true);
            foreach (IVariantContent variant in variants)
                variant.Populate(choices);

            gameObject.SetActive(true);
            SeekAnimator(0f);
        }
        else if (!hideOnRevisit)
        {
            // Revisit, show frozen — snap to the end frame without animating.
            gameObject.SetActive(true);
            SeekAnimator(1f);
        }
        // else: hideOnRevisit = true → stay deactivated. IsBlocking is false so ComicPanel auto-chains.
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