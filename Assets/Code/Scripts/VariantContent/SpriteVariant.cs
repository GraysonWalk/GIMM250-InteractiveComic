using UnityEngine;

/// <summary>
///     Swaps the SpriteRenderer's sprite based on the player's focus choices.
///     Attach to any child GameObject of an AnimatedStep that has a SpriteRenderer.
///     AnimatedStep discovers this component via IVariantContent and calls Populate() at activation time.
///
///     Designer workflow:
///     1. Add this component to a child GameObject of an AnimatedStep.
///     2. Assign a Default Sprite — shown when no override applies.
///     3. Assign override sprites for any focus choices that change this element's art.
///     4. Leave override fields empty to fall through to the default.
///
///     Priority order matches PanelText: Science → Philosophy → Leadership, OptionA before OptionB.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class SpriteVariant : MonoBehaviour, IVariantContent
{
    #region Variables

    [Tooltip("Sprite shown when no focus choice has been made, or when no override applies.")]
    [SerializeField] private Sprite defaultSprite;

    [Header("Science Variants")]
    [Tooltip("Shown when the player chose Science Option A.")]
    [SerializeField] private Sprite scienceOptionASprite;
    [Tooltip("Shown when the player chose Science Option B.")]
    [SerializeField] private Sprite scienceOptionBSprite;

    [Header("Philosophy Variants")]
    [Tooltip("Shown when the player chose Philosophy Option A.")]
    [SerializeField] private Sprite philosophyOptionASprite;
    [Tooltip("Shown when the player chose Philosophy Option B.")]
    [SerializeField] private Sprite philosophyOptionBSprite;

    [Header("Leadership Variants")]
    [Tooltip("Shown when the player chose Leadership Option A.")]
    [SerializeField] private Sprite leadershipOptionASprite;
    [Tooltip("Shown when the player chose Leadership Option B.")]
    [SerializeField] private Sprite leadershipOptionBSprite;

    private SpriteRenderer _renderer = null!; // Assigned in Awake() via GetComponent — guaranteed by [RequireComponent]

    #endregion

    #region Methods

    private void Awake()
    {
        _renderer = GetComponent<SpriteRenderer>();
    }

    /// <summary>
    ///     Sets the sprite based on the player's current focus choices.
    ///     Checks each category in order (Science → Philosophy → Leadership) and uses the first
    ///     override with a non-null value. Falls back to defaultSprite if nothing matches.
    ///     Called by AnimatedStep.Activate() before the animation plays.
    /// </summary>
    public void Populate(PlayerChoicesSO choices)
    {
        if (choices != null)
        {
            if (choices.ScienceFocus == ScienceChoice.OptionA && scienceOptionASprite != null)
            { _renderer.sprite = scienceOptionASprite; return; }

            if (choices.ScienceFocus == ScienceChoice.OptionB && scienceOptionBSprite != null)
            { _renderer.sprite = scienceOptionBSprite; return; }

            if (choices.PhilosophyFocus == PhilosophyChoice.OptionA && philosophyOptionASprite != null)
            { _renderer.sprite = philosophyOptionASprite; return; }

            if (choices.PhilosophyFocus == PhilosophyChoice.OptionB && philosophyOptionBSprite != null)
            { _renderer.sprite = philosophyOptionBSprite; return; }

            if (choices.LeadershipFocus == LeadershipChoice.OptionA && leadershipOptionASprite != null)
            { _renderer.sprite = leadershipOptionASprite; return; }

            if (choices.LeadershipFocus == LeadershipChoice.OptionB && leadershipOptionBSprite != null)
            { _renderer.sprite = leadershipOptionBSprite; return; }
        }

        if (defaultSprite != null)
            _renderer.sprite = defaultSprite;
    }

    #endregion
}
