using UnityEngine;
using UnityEngine.Events;

/// <summary>
///     Represents a focus point in the comic panel where the player can make a choice. Each focus point has a type, an
///     option, and an event that triggers when the player selects it.
/// </summary>
/// TODO: Turn into IPanelStep
/// TODO: Add fields for FocusType and PlayerChoicesSO
/// TODO: Add logic to show choice buttons and wait
/// TODO: Add ability to set focus on PlayerChoicesSO
public class FocusPoint : MonoBehaviour
{
    private FocusType _focusType;
    private UnityEvent _onFocusSelected;
    private FocusOption _option;
}