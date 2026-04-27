using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
///     Manages the flow of comic panels, including advancing, retreating, and replaying panels, and tracking loop counts
///     for panel eligibility.
/// </summary>
public class ComicManager : MonoBehaviour
{
    #region Variables

    [SerializeField] private List<ComicPanel> comicPanels;
    [SerializeField] private CinemachineBrain brain;

    private ComicPanel _currentComicPanel;
    private IComicPanel _displayedPanel;
    private LoopCount _currentLoopCount;
    private readonly CommandHistory _commandHistory = new();

    /// <summary>
    ///     Relayed from the current panel's OnReadyForInput.
    ///     Fires after intro animation and after each blocking step completes.
    ///     NavigationController subscribes to unblock advance button input.
    ///     NavigationPresenter subscribes to show the "press spacebar" hint.
    /// </summary>
    public UnityEvent OnCurrentPanelReadyForInput { get; } = new();

    /// <summary>
    ///     Fired whenever the displayed panel changes.
    ///     true = panel was already seen (show replay button); false = first visit (hide replay button).
    /// </summary>
    public UnityEvent<bool> OnReplayAvailabilityChanged { get; } = new();

    /// <summary>
    ///     Fired after every history change (Execute, Undo, Redo).
    ///     First bool = canGoBack, second bool = canGoForward.
    ///     NavigationPresenter uses this to enable/disable arrows and derive advance availability.
    /// </summary>
    public UnityEvent<bool, bool> OnNavigationAvailabilityChanged { get; } = new();

    #endregion

    #region Methods

    private void Start()
    {
        // Subscribe to and show the first eligible panel — the only manual subscription needed.
        // All subsequent panels are subscribed to via SwitchToPanel() as the comic advances.
        ComicPanel first = PanelSelector.NextPanel(comicPanels, null, _currentLoopCount);
        if (first != null)
            SwitchToPanel(first);
        else
            Debug.LogError("[ComicManager] No panels found for Loop0. Check PanelDataSO firstLoop values.", this);
    }

    private void OnValidate()
    {
        if (brain == null)
            Debug.LogError("[ComicManager] CinemachineBrain (brain) is not assigned. " +
                           "Drag the Main Camera's CinemachineBrain component into this slot.", this);
    }

    /// <summary>
    ///     Called by advance button (NavigationController.Advance()).
    ///     Blocked when the player is browsing history (CanRedo = true) — advance button only
    ///     drives the story forward from the current story position.
    /// </summary>
    public void AdvanceComic()
    {
        if (_commandHistory.CanRedo) return; // browsing history — advance button disabled
        _currentComicPanel.Advance();
    }

    /// <summary>
    ///     Called by back button (NavigationController.RetreatComic()).
    ///     Moves backward through history, showing the previous panel in its end state with no animation
    /// </summary>
    public void RetreatComic()
    {
        ICommand cmd = _commandHistory.Undo();
        if (cmd is not SwitchCameraCommand switchCmd) return;
        _displayedPanel = switchCmd.PanelAfterUndo;
        OnReplayAvailabilityChanged.Invoke(true);
        BroadcastNavigationAvailability();
    }

    /// <summary>
    ///     Called by forward button (NavigationController.NextPanel()).
    ///     Moves forward through history, showing the next panel in its end state with no animation.
    /// </summary>
    public void RedoPanel()
    {
        ICommand cmd = _commandHistory.Redo();
        if (cmd is not SwitchCameraCommand switchCmd) return;
        _displayedPanel = switchCmd.PanelAfterExecute;
        OnReplayAvailabilityChanged.Invoke(true);
        BroadcastNavigationAvailability();

        // If there's nothing left to redo we're back at the story front.
        // ShowInstant() never fires OnReadyForInput, so fire it here to show the advance hint.
        if (!_commandHistory.CanRedo)
            OnCurrentPanelReadyForInput.Invoke();
    }

    /// <summary>
    ///     Called by replay button (NavigationPresenter.ReplayButton).
    ///     Replays the current panel's intro animation and steps.
    /// </summary>
    public void ReplayCurrentPanel()
    {
        _displayedPanel?.Show();
        OnReplayAvailabilityChanged.Invoke(false);
    }

    /// <summary>
    ///     Moves the OnPanelComplete and OnReadyForInput subscriptions from whatever panel
    ///     was current to <paramref name="next" />.
    /// </summary>
    private void RewireCurrentPanel(ComicPanel next)
    {
        if (_currentComicPanel != null)
        {
            _currentComicPanel.OnPanelComplete.RemoveListener(MoveToNextPanel);
            _currentComicPanel.OnReadyForInput.RemoveListener(OnCurrentPanelReadyForInput.Invoke);
        }

        _currentComicPanel = next;

        if (_currentComicPanel == null) return;
        _currentComicPanel.OnPanelComplete.AddListener(MoveToNextPanel);
        _currentComicPanel.OnReadyForInput.AddListener(OnCurrentPanelReadyForInput.Invoke);
    }

    /// <summary>
    ///     Switches to a new panel, routes the camera switch through CommandHistory
    ///     so the transition is undoable and redoable.
    /// </summary>
    private void SwitchToPanel(ComicPanel next)
    {
        ComicPanel prev = _currentComicPanel;
        RewireCurrentPanel(next);

        // First launch always cuts so the main camera doesn't travel through empty space.
        // All subsequent transitions use the target panel's configured incoming blend.
        CinemachineBlendDefinition blend = prev == null
            ? new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.Cut, 0f)
            : next.IncomingBlend;

        // Capture whether this panel was already seen BEFORE Show() runs —
        // Show() sets HasBeenVisited = true on first visit, so we must read it first.
        bool isRevisit = next.HasBeenVisited;

        _commandHistory.Execute(new SwitchCameraCommand(prev, next, brain, blend));
        _displayedPanel = next;
        OnReplayAvailabilityChanged.Invoke(isRevisit);
        BroadcastNavigationAvailability();
    }

    /// <summary>
    ///     Subscribed to the current panel's OnPanelComplete event.
    ///     Selects the next panel, advances the loop count if wrapping, and switches to it.
    /// </summary>
    private void MoveToNextPanel()
    {
        ComicPanel next = PanelSelector.NextPanel(comicPanels, _currentComicPanel, _currentLoopCount);

        if (next == null)
        {
            Debug.LogWarning("[ComicManager] No eligible panels found. Check panel data and loop count.", this);
            return;
        }

        // A wrap-around means the current panel was the last in this loop — advance the loop count.
        bool wrapped = next.Rank <= _currentComicPanel.Rank;
        if (wrapped && _currentLoopCount < LoopCount.Loop3)
        {
            _currentLoopCount++;
            // Re-query with the incremented loop count so newly-unlocked panels are included.
            next = PanelSelector.NextPanel(comicPanels, null, _currentLoopCount);
            if (next == null)
            {
                Debug.LogWarning("[ComicManager] No eligible panels found after loop increment.", this);
                return;
            }
        }

        SwitchToPanel(next);
    }

    private void BroadcastNavigationAvailability()
    {
        OnNavigationAvailabilityChanged.Invoke(_commandHistory.CanUndo, _commandHistory.CanRedo);
    }

    #endregion
}