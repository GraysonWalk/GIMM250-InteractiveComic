using TMPro;
using UnityEngine;

/// <summary>
///     Displays focus-variant text (dialogue, prose, captions) on a child GameObject of an AnimatedStep.
///     Visibility is controlled by the AnimatedStep's Animator via keyframes — this component
///     only selects and sets the correct text string when the step activates.
///
///     Multiple PanelText components can exist under a single AnimatedStep, each
///     with independent text variants and independent Animator-driven show/hide timing.
///
///     Designer workflow:
///     1. Add a child GameObject under the AnimatedStep; attach this component.
///     2. Fill in defaultText, and any focus variant overrides needed.
///     3. Animate the child's visibility in the AnimatedStep's Animator Controller.
///     4. Save the child as inactive in Prefab Mode to prevent spurious TMP editor warnings.
/// </summary>
[RequireComponent(typeof(TextMeshPro))]
public class PanelText : MonoBehaviour, IVariantContent
{
    #region Variables

    [Tooltip("Text shown when no focus choice has been made, or when no override applies.")]
    [SerializeField] private string defaultText;

    [Header("Science Variants")]
    [Tooltip("Shown when the player chose Science Option A.")]
    [SerializeField] private string scienceOptionAText;
    [Tooltip("Shown when the player chose Science Option B.")]
    [SerializeField] private string scienceOptionBText;

    [Header("Philosophy Variants")]
    [Tooltip("Shown when the player chose Philosophy Option A.")]
    [SerializeField] private string philosophyOptionAText;
    [Tooltip("Shown when the player chose Philosophy Option B.")]
    [SerializeField] private string philosophyOptionBText;

    [Header("Leadership Variants")]
    [Tooltip("Shown when the player chose Leadership Option A.")]
    [SerializeField] private string leadershipOptionAText;
    [Tooltip("Shown when the player chose Leadership Option B.")]
    [SerializeField] private string leadershipOptionBText;

    private TextMeshPro _label = null!; // Assigned in Awake() via GetComponent — guaranteed by [RequireComponent]

    #endregion

    #region Methods

    private void Awake()
    {
        _label = GetComponent<TextMeshPro>();
    }

    /// <summary>
    ///     Sets the displayed text based on the player's current focus choices.
    ///     Checks each category in order (Science → Philosophy → Leadership) and uses the first
    ///     override with a non-empty value. Falls back to defaultText if nothing matches.
    ///     Called by AnimatedStep.Activate() before the animation plays.
    /// </summary>
    public void Populate(PlayerChoicesSO choices)
    {
        if (choices != null)
        {
            if (choices.ScienceFocus == ScienceChoice.OptionA && !string.IsNullOrEmpty(scienceOptionAText))
            { _label.text = scienceOptionAText; return; }

            if (choices.ScienceFocus == ScienceChoice.OptionB && !string.IsNullOrEmpty(scienceOptionBText))
            { _label.text = scienceOptionBText; return; }

            if (choices.PhilosophyFocus == PhilosophyChoice.OptionA && !string.IsNullOrEmpty(philosophyOptionAText))
            { _label.text = philosophyOptionAText; return; }

            if (choices.PhilosophyFocus == PhilosophyChoice.OptionB && !string.IsNullOrEmpty(philosophyOptionBText))
            { _label.text = philosophyOptionBText; return; }

            if (choices.LeadershipFocus == LeadershipChoice.OptionA && !string.IsNullOrEmpty(leadershipOptionAText))
            { _label.text = leadershipOptionAText; return; }

            if (choices.LeadershipFocus == LeadershipChoice.OptionB && !string.IsNullOrEmpty(leadershipOptionBText))
            { _label.text = leadershipOptionBText; return; }
        }

        if (!string.IsNullOrEmpty(defaultText))
            _label.text = defaultText;
    }

    /// <summary>
    ///     Called by Unity when the component is first added in the Editor.
    ///     Starts inactive so TextMeshPro does not attempt to render in the Scene View
    ///     before fonts are loaded, preventing spurious "No Font Asset" warnings on scene open.
    /// </summary>
    private void Reset()
    {
        gameObject.SetActive(false);
    }

    #endregion
}