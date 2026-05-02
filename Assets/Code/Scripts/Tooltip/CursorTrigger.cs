using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
///     Changes the OS cursor when the pointer enters this element and restores it on exit.
///     Add alongside <see cref="TooltipTrigger" /> on any hoverable element — the two
///     components are independent and can be used together or separately.
/// </summary>
public class CursorTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    #region Variables

    [Tooltip("Texture to display as the cursor. Import with Read/Write enabled and " +
             "Texture Type set to Cursor in the Import Settings.")]
    [SerializeField] private Texture2D cursorTexture;

    [Tooltip("The pixel within the texture that maps to the actual click point. " +
             "(0, 0) = top-left corner, suitable for a standard arrow cursor.")]
    [SerializeField] private Vector2 hotspot = Vector2.zero;

    // ForceSoftware is required on macOS — CursorMode.Auto uses hardware cursors
    // which the Mac Editor intercepts, preventing custom cursors from appearing.
    private const CursorMode Mode = CursorMode.ForceSoftware;

    #endregion

    #region Methods

    public void OnPointerEnter(PointerEventData eventData) => Cursor.SetCursor(cursorTexture, hotspot, Mode);

    public void OnPointerExit(PointerEventData eventData) => ResetCursor();

    private void OnDisable() => ResetCursor();

    private static void ResetCursor() => CursorManager.ApplyDefault();

    #endregion
}
