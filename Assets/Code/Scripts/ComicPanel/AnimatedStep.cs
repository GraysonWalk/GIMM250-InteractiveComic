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

    [Header("Variant Text Overrides (ignored if Label is unassigned)")]
    [Tooltip("Overrides the TMP_Text value when the player has chosen Science focus.")]
    [SerializeField] private string scienceText;

    [Tooltip("Overrides the TMP_Text value when the player has chosen Philosophy focus.")]
    [SerializeField] private string philosophyText;

    [Tooltip("Overrides the TMP_Text value when the player has chosen Leadership focus.")]
    [SerializeField] private string leadershipText;

    // Blocking — Advance() is locked until OnAnimationFinished fires.
    public bool IsBlocking => true;
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
    ///     Activates this step. Applies variant text if a label is assigned and a
    ///     matching focus choice is set; otherwise leaves the label text unchanged.
    /// </summary>
    public void Activate(PlayerChoicesSO choices)
    {
        if (label != null && choices != null)
        {
            if (choices.ScienceFocus != FocusOption.None && !string.IsNullOrEmpty(scienceText))
                label.text = scienceText;
            else if (choices.PhilosophyFocus != FocusOption.None && !string.IsNullOrEmpty(philosophyText))
                label.text = philosophyText;
            else if (choices.LeadershipFocus != FocusOption.None && !string.IsNullOrEmpty(leadershipText))
                label.text = leadershipText;
        }

        gameObject.SetActive(true);
    }

    /// <summary>Hides this element.</summary>
    public void Deactivate()
    {
        gameObject.SetActive(false);
    }

    #endregion
}