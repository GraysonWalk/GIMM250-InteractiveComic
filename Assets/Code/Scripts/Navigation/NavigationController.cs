using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
///     Handles the navigation between comic panels.
///     Advance() (tied to Advance Input - spacebar) drives the story forward with animations.
///     NextPanel() and PreviousPanel() are called by UI arrow buttons to jump
///     between already-visited panels instantly, with no animations.
/// </summary>
public class NavigationController : MonoBehaviour, INavigationController
{
    #region Variables

    [SerializeField] private ComicManager comicManager;
    [SerializeField] private CinemachineBrain brain;

    // True while a camera blend or panel intro animation is in progress.
    // Only Advance() blocks input — history navigation (UI arrows) is always instant.
    private bool _inputBlocked;

    private InputAction _advanceAction;

    #endregion

    #region Methods

    private void Awake()
    {
        _advanceAction = InputSystem.actions.FindAction("Advance");
    }

    private void OnEnable()
    {
        _advanceAction.performed += OnAdvance;
        CinemachineCore.CameraActivatedEvent.AddListener(OnCameraActivated);
        comicManager.OnCurrentPanelReadyForInput.AddListener(OnPanelReadyForInput);
    }

    private void OnDisable()
    {
        _advanceAction.performed -= OnAdvance;
        CinemachineCore.CameraActivatedEvent.RemoveListener(OnCameraActivated);
        comicManager.OnCurrentPanelReadyForInput.RemoveListener(OnPanelReadyForInput);
    }

    /// <summary>
    ///     Steps forward within the current panel (more dialogue/images), or advances to
    ///     the next panel if the current panel is complete. Plays animations. Blocks further input
    ///     until the camera blend and panel animation finish.
    /// </summary>
    public void Advance()
    {
        if (_inputBlocked) return;
        _inputBlocked = true;
        comicManager.AdvanceComic();
    }

    /// <summary>
    ///     Right UI arrow button. Moves forward through already-visited panels in history,
    ///     showing each in its end state with no animation. Never blocks input.
    /// </summary>
    public void NextPanel()
    {
        comicManager.RedoPanel();
    }

    /// <summary>
    ///     Left UI arrow button. Moves back through already-visited panels in history,
    ///     showing each in its end state with no animation. Never blocks input.
    /// </summary>
    public void PreviousPanel()
    {
        comicManager.RetreatComic();
    }

    /// <summary>
    ///     Called whenever the current panel is ready for the next Advance Input button press:
    ///     intro animation finished, non-blocking step activated, or blocking step completed.
    /// </summary>
    private void OnPanelReadyForInput()
    {
        _inputBlocked = false;
    }

    private void OnAdvance(InputAction.CallbackContext ctx)
    {
        Advance();
    }

    // Unblock input once the Cinemachine blend is done and the new camera is fully active
    private void OnCameraActivated(ICinemachineCamera.ActivationEventParams args)
    {
        if (!brain.IsBlending)
            _inputBlocked = false;
    }

    #endregion
}