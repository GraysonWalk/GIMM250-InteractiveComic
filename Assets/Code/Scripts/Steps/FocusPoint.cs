using UnityEngine;

/// <summary>
///     A blocking step that presents two focus choice options to the player.
///     When activated for the first time, wires two ClickableOption children as interactive
///     click targets and waits for the player to choose one. The result is written to
///     PlayerChoicesSO and affects art and text in later panels via IVariantContent.
///
///     On replay or revisit: shows the already-chosen state without re-presenting the choice.
///     PrepareForReplay() is intentionally a no-op — choices persist across replays.
///
///     The options (optionA, optionB) can be any GameObjects anywhere in the hierarchy
///     with a ClickableOption component attached.
/// </summary>
public class FocusPoint : StepBase
{
    #region Variables

    [Tooltip("Which focus axis this step controls.")]
    [SerializeField] private FocusCategory category;

    [Tooltip("The shared PlayerChoices asset. All panels reference the same asset.")]
    [SerializeField] private PlayerChoicesSO choices;

    [Tooltip("The object the player clicks to select Option A.")]
    [SerializeField] private ClickableOption optionA;

    [Tooltip("The object the player clicks to select Option B.")]
    [SerializeField] private ClickableOption optionB;

    // True once a choice has been made; used by ShowInFinalState to show this step in history view.
    public override bool ShowInFinalState => HasBeenActivated;
    public override void Activate(PlayerChoicesSO choice)
    {
        throw new System.NotImplementedException();
    }

    #endregion
}
