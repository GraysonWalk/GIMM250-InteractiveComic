using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
///     Quits the application when the Escape key is pressed.
///     In the Unity editor, exits Play Mode instead of closing the application.
///
///     Attach to any persistent GameObject in the scene (e.g. the ComicManager GameObject).
/// </summary>
public class QuitAction : MonoBehaviour
{
    #region Variables

    private InputAction _quitAction;

    #endregion

    #region Methods

    private void Awake()
    {
        _quitAction = new InputAction(binding: "<Keyboard>/escape");
    }

    private void OnEnable()
    {
        _quitAction.performed += OnQuit;
        _quitAction.Enable();
    }

    private void OnDisable()
    {
        _quitAction.performed -= OnQuit;
        _quitAction.Disable();
    }

    private void OnDestroy()
    {
        _quitAction.Dispose();
    }

    private static void OnQuit(InputAction.CallbackContext context)
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    #endregion
}
