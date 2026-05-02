using System;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
///     Attach to any hoverable element — navigation buttons, panel hotspots, focus prompts, etc.
///     Fill in the message field only. No other wiring or asset assignment required.
/// </summary>
public class TooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    #region Variables

    [Tooltip("Text shown in the tooltip when the player hovers over this element.")]
    [SerializeField] private string message;

    public static event Action<string> OnShow;
    public static event Action OnHide;

    #endregion

    #region Methods

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!string.IsNullOrEmpty(message))
            OnShow?.Invoke(message);
    }

    public void OnPointerExit(PointerEventData eventData) => OnHide?.Invoke();

    private void OnDisable() => OnHide?.Invoke();

    #endregion
}