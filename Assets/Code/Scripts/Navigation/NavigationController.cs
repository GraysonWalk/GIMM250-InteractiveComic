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

    // True while a panel intro animation or blocking step is in progress.
    // Set to true on Advance(); unblocked only when the panel fires OnReadyForInput,
    // ensuring input is never accepted mid-animation.
    private bool _inputBlocked;

    private InputAction _advanceAction;
    private InputAction _navigateBackAction;
    private InputAction _navigateForwardAction;

    #endregion

    #region Methods

    private void Awake()
    {
        _advanceAction         = InputSystem.actions.FindAction("Advance");
        _navigateBackAction    = InputSystem.actions.FindAction("NavigateBack");
        _navigateForwardAction = InputSystem.actions.FindAction("NavigateForward");
    }

    private void OnEnable()
    {
        _advanceAction.performed         += OnAdvance;
        _navigateBackAction.performed    += OnNavigateBack;
        _navigateForwardAction.performed += OnNavigateForward;
        comicManager.OnCurrentPanelReadyForInput.AddListener(OnPanelReadyForInput);
    }

    private void OnDisable()
    {
        _advanceAction.performed         -= OnAdvance;
        _navigateBackAction.performed    -= OnNavigateBack;
        _navigateForwardAction.performed -= OnNavigateForward;
        comicManager.OnCurrentPanelReadyForInput.RemoveListener(OnPanelReadyForInput);
    }

    /// <summary>
    ///     Steps forward within the current panel (more dialogue/images), or advances to
    ///     the next panel if the current panel is complete. Plays animations. Blocks further input
    ///     until the panel's intro animation and any blocking steps finish.
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
    private void OnPanelReadyForInput() => _inputBlocked = false;
    private void OnAdvance(InputAction.CallbackContext ctx) => Advance();
    private void OnNavigateBack(InputAction.CallbackContext ctx) => PreviousPanel();
    private void OnNavigateForward(InputAction.CallbackContext ctx) => NextPanel();

    #endregion
}