/// <summary>
///     Implemented by any child component of an AnimatedStep that needs to change its content
///     based on the player's focus choices (e.g. PanelText, SpriteVariant).
///
///     AnimatedStep discovers all IVariantContent children via GetComponentsInChildren and calls
///     Populate() at activation time. To add a new kind of choice-driven content, implement this
///     interface on a new component — AnimatedStep requires no changes.
/// </summary>
public interface IVariantContent
{
    /// <summary>Selects and applies the appropriate content variant for the given choices.</summary>
    void Populate(PlayerChoicesSO choices);
}
