using UnityEngine;

/// <summary>
///     Sets the default game cursor on startup.
///     Required on macOS — Project Settings → Player → Default Cursor only applies to
///     standalone builds and is ignored in the Editor.
///     Place on any persistent GameObject (e.g. the same one as ComicManager).
/// </summary>
public class CursorManager : MonoBehaviour
{
    #region Variables

    [Tooltip("The cursor shown by default throughout the game. " +
             "Import with Texture Type set to Cursor in the Import Settings.")]
    [SerializeField] private Texture2D defaultCursor;

    [Tooltip("The pixel within the texture that maps to the actual click point. " +
             "(0, 0) = top-left corner, suitable for a standard arrow cursor.")]
    [SerializeField] private Vector2 hotspot = Vector2.zero;

    /// <summary>
    ///     The active default cursor texture. <see cref="CursorTrigger" /> uses this
    ///     when restoring the cursor after a hover ends.
    /// </summary>
    public static Texture2D DefaultCursor { get; private set; }

    /// <summary> The hotspot matching the default cursor texture. </summary>
    public static Vector2 DefaultHotspot { get; private set; }

    #endregion

    #region Methods

    private void Awake()
    {
        DefaultCursor  = defaultCursor;
        DefaultHotspot = hotspot;
        ApplyDefault();
    }

    /// <summary> Restores the default cursor. Called by <see cref="CursorTrigger" /> on exit. </summary>
    public static void ApplyDefault()
    {
        Cursor.SetCursor(DefaultCursor, DefaultHotspot, CursorMode.ForceSoftware);
    }

    #endregion
}
