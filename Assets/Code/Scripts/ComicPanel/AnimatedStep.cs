using TMPro;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
///     A blocking animated step. Attach to any child GameObject of a ComicPanel.
///     Each step plays its animation when activated and locks Advance() until
///     OnAnimationFinished() is called by the Animation Event on the last keyframe.
///     Optionally assign a TMP_Text label to display dialogue alongside the animation.
///     Leave label unassigned for purely visual steps (sprite reveals, panel effects, etc.).
///     Designer workflow:
///     1. Add a child GameObject under ComicPanel; attach this component + an Animator.
///     2. Set the Animator Controller's default state to your clip; disable Loop Time.
///     3. On the last keyframe add an Animation Event pointing to OnAnimationFinished().
///     4. Optionally: assign a TMP_Text for dialogue and fill in variant overrides.
///     5. Tick "Persists In Final State" if this element should remain visible at the panel's end.
/// </summary>
public sealed class AnimatedStep : MonoBehaviour, IPanelStep
{
    #region Variables

    [Tooltip("Optional — assign a TMP_Text to display dialogue text on this step.")]
    [SerializeField] private TMP_Text label;

    [Tooltip("If ticked, this element remains visible when the panel reaches its final state.")]
    [SerializeField] private bool persistsInFinalState;

    [Tooltip("If ticked, this step replays its animation when the panel is replayed. " +
             "Untick for steps that should never repeat (e.g. a choice prompt already answered).")]
    [SerializeField] private bool replayOnRevisit = true;

    [Header("Variant Text Overrides (ignored if Label is unassigned)")]
    [Tooltip("Overrides the TMP_Text value when the player has chosen Science focus.")]
    [SerializeField] private string scienceText;

    [Tooltip("Overrides the TMP_Text value when the player has chosen Philosophy focus.")]
    [SerializeField] private string philosophyText;

    [Tooltip("Overrides the TMP_Text value when the player has chosen Leadership focus.")]
    [SerializeField] private string leadershipText;

    // True after the first Activate() call. Combined with replayOnRevisit to decide
    // whether to block input and animate, or skip instantly on subsequent visits.
    private bool _hasBeenActivated;

    // Blocks input until OnAnimationFinished fires — unless this step has already run
    // and replayOnRevisit is false, in which case it is skipped without blocking.
    public bool IsBlocking => !_hasBeenActivated || replayOnRevisit;
    public bool PersistsInFinalState => persistsInFinalState;
    public UnityEvent OnStepComplete { get; } = new();

    #endregion

    #region Methods

    /// <summary>
    ///     Called by an Animation Event on the last frame of this step's clip.
    ///     Fires OnStepComplete so ComicPanel can unblock input.
    /// </summary>
    public void OnAnimationFinished()
    {
        OnStepComplete.Invoke();
    }

    /// <summary>
    ///     Activates this step. On first visit (or replay): applies variant text and plays animation.
    ///     On revisit when replayOnRevisit is false: shows the element in its last-frame state instantly,
    ///     without blocking input or replaying the animation.
    /// </summary>
    public void Activate(PlayerChoicesSO choices)
    {
        bool skip = _hasBeenActivated && !replayOnRevisit;
        _hasBeenActivated = true;

        // Only update text when actually playing — preserves the text from the original visit
        // when skipping, since a different choice may now be active.
        if (!skip && label != null && choices != null)
        {
            if (choices.ScienceFocus != FocusOption.None && !string.IsNullOrEmpty(scienceText))
                label.text = scienceText;
            else if (choices.PhilosophyFocus != FocusOption.None && !string.IsNullOrEmpty(philosophyText))
                label.text = philosophyText;
            else if (choices.LeadershipFocus != FocusOption.None && !string.IsNullOrEmpty(leadershipText))
                label.text = leadershipText;
        }

        // Enabling the GameObject lets the Animator resume from where it was last left.
        // For skipped steps this is the end frame; for replayed steps ComicPanel.Replay()
        // will have reset _hasBeenActivated so the full animation plays again.
        gameObject.SetActive(true);
    }

    /// <summary>Hides this element.</summary>
    public void Deactivate()
    {
        gameObject.SetActive(false);
    }

    /// <summary>
    ///     Resets visited state if this step should replay, so it blocks input and
    ///     animates again on the next Activate(). Steps with replayOnRevisit = false
    ///     are left untouched — they will still skip on the next pass.
    /// </summary>
    public void PrepareForReplay()
    {
        if (replayOnRevisit)
            _hasBeenActivated = false;
    }

    #endregion
}