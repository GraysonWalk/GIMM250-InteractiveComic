using System;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
///     A blocking step that presents two focus choice options to the player.
///     When activated for the first time, enables both Clickable options and waits for the player
///     to choose one. The result is written to PlayerChoicesSO and affects art and text in later
///     panels via IVariantContent.
///
///     On replay or revisit: shows the already-chosen state without re-presenting the choice.
///     PrepareForReplay() is intentionally a no-op — choices persist across replays.
///
///     Global Volume change: assign a scene Volume and two VolumeProfiles (one per option).
///     The matching profile is applied at the moment the player chooses and persists for the
///     rest of the comic. On revisit the correct profile is re-applied so history browsing
///     reflects the player's choice. Leave Volume empty for panels with no post-process change.
///
///     Wiring in the Inspector:
///     1. Add this component to a child GameObject of a ComicPanel.
///     2. Set Category to the focus axis this step controls (Science / Philosophy / Leadership).
///     3. Assign Choices (the shared PlayerChoices.asset).
///     4. Create two child GameObjects with 3D Colliders; attach Clickable to each.
///     5. Assign them to the Option A and Option B fields.
///     6. On optionA's Clickable.onClick, add a listener → FocusPoint.RecordChoice → check = true.
///     7. On optionB's Clickable.onClick, add a listener → FocusPoint.RecordChoice → check = false.
///     8. Ensure a ClickManager is present in the scene to route Physics raycasts to Clickable.
///     9. Optionally assign a Global Volume and two VolumeProfiles for a persistent look change.
///
///     Note: Clickable uses Physics.Raycast — option GameObjects must have a 3D Collider.
///     UI or 2D sprite options are not supported with this system.
/// </summary>
public class FocusPoint : StepBase
{
    #region Variables

    [Tooltip("Which focus axis this step controls.")]
    [SerializeField] private FocusCategory category;

    [Tooltip("The shared PlayerChoices asset. All panels reference the same asset.")]
    [SerializeField] private PlayerChoicesSO choices;

    [Tooltip("The Clickable on the Option A object. Wire its onClick → RecordChoice with true.")]
    [SerializeField] private Clickable optionA;

    [Tooltip("The Clickable on the Option B object. Wire its onClick → RecordChoice with false.")]
    [SerializeField] private Clickable optionB;

    [Header("Global Volume Change (optional)")]
    [Tooltip("Scene Volume to update when the player chooses. Leave empty for no post-process change.")]
    [SerializeField] private Volume globalVolume;

    [Tooltip("Profile applied to the global Volume when the player picks Option A.")]
    [SerializeField] private VolumeProfile optionAProfile;

    [Tooltip("Profile applied to the global Volume when the player picks Option B.")]
    [SerializeField] private VolumeProfile optionBProfile;

    // True once a choice has been made; used by ShowInFinalState to show this step in history view.
    public override bool ShowInFinalState => HasBeenActivated;

    // FocusPoint interaction is click-based; the spacebar advance hint should not appear while
    // this step is queued. The player clicks an option — there is no "press spacebar" action.
    public override bool ShowAdvanceHint => false;

    #endregion

    #region Methods

    private void Awake()
    {
        if (choices == null)
            Debug.LogError("[FocusPoint] PlayerChoicesSO (choices) is not assigned.", this);
        if (optionA == null)
            Debug.LogError("[FocusPoint] Option A (Clickable) is not assigned.", this);
        if (optionB == null)
            Debug.LogError("[FocusPoint] Option B (Clickable) is not assigned.", this);
    }

    /// <summary>
    ///     First visit: enables both Clickable options and blocks until the player chooses.
    ///     Revisit or replay: shows the already-chosen state with the unchosen option disabled
    ///     and re-applies the global Volume profile so history browsing is visually consistent.
    /// </summary>
    public override void Activate(PlayerChoicesSO _)
    {
        // FocusPoint writes to its own [SerializeField] choices rather than the passed parameter —
        // both reference the same shared asset, but using the field clarifies write vs read intent.
        bool skip = BeginActivation();
        gameObject.SetActive(true);

        if (!skip)
        {
            // First visit — enable both options for interaction.
            if (optionA != null) optionA.enabled = true;
            if (optionB != null) optionB.enabled = true;
        }
        else
        {
            // Revisit — show the chosen state; disable the unchosen option so it cannot be re-selected.
            bool choseA = WasOptionAChosen();
            if (optionA != null) optionA.enabled = choseA;
            if (optionB != null) optionB.enabled = !choseA;

            // Re-apply the volume profile so the comic looks correct when browsing history.
            ApplyGlobalVolume();
        }
    }

    /// <summary>
    ///     Disables both Clickables and hides this step.
    ///     Prevents clicks from registering while the panel is hidden.
    /// </summary>
    public override void Deactivate()
    {
        if (optionA != null) optionA.enabled = false;
        if (optionB != null) optionB.enabled = false;
        base.Deactivate();
    }

    /// <summary>
    ///     Intentional no-op. FocusPoint preserves its chosen state across explicit replays —
    ///     the player sees which option they picked rather than being re-prompted.
    ///     _hasBeenActivated is left true so Activate() takes the already-chosen (skip) path.
    /// </summary>
    public override void PrepareForReplay() { }

    /// <summary>
    ///     Records the player's choice and advances the step.
    ///     Called by Clickable.onClick (UnityEvent) — wire in the Inspector with a constant bool:
    ///     optionA.onClick → RecordChoice (true), optionB.onClick → RecordChoice (false).
    /// </summary>
    public void RecordChoice(bool isOptionA)
    {
        // Disable both immediately to prevent double-firing if clicked again before the panel advances.
        if (optionA != null) optionA.enabled = false;
        if (optionB != null) optionB.enabled = false;

        // Write choice first so ApplyGlobalVolume() sees the new value.
        switch (category)
        {
            case FocusCategory.Science:
                choices.SetScienceFocus(isOptionA ? ScienceChoice.OptionA : ScienceChoice.OptionB);
                break;
            case FocusCategory.Philosophy:
                choices.SetPhilosophyFocus(isOptionA ? PhilosophyChoice.OptionA : PhilosophyChoice.OptionB);
                break;
            case FocusCategory.Leadership:
                choices.SetLeadershipFocus(isOptionA ? LeadershipChoice.OptionA : LeadershipChoice.OptionB);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        ApplyGlobalVolume();
        OnStepComplete.Invoke();
    }

    // Applies the correct VolumeProfile to the global Volume based on the current stored choice.
    // Called at decision time and again on revisit to keep the look consistent during history browsing.
    private void ApplyGlobalVolume()
    {
        if (globalVolume == null) return;
        VolumeProfile profile = WasOptionAChosen() ? optionAProfile : optionBProfile;
        if (profile != null)
            globalVolume.profile = profile;
    }

    // Returns true if the stored choice for this category is OptionA.
    private bool WasOptionAChosen()
    {
        return category switch
        {
            FocusCategory.Science    => choices.ScienceFocus    == ScienceChoice.OptionA,
            FocusCategory.Philosophy => choices.PhilosophyFocus == PhilosophyChoice.OptionA,
            FocusCategory.Leadership => choices.LeadershipFocus == LeadershipChoice.OptionA,
            _                        => false
        };
    }

    #endregion
}
