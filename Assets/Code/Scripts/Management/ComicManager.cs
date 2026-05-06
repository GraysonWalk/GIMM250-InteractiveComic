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

    // comicPanels is populated automatically in Start() via FindObjectsByType —
    // do NOT add a [SerializeField] list. Manual Inspector wiring causes null slots
    // after merges because multiple teammates modify the same list in Main.unity.
    private IComicPanel[] _comicPanels;

    [SerializeField] private CinemachineBrain brain;

    private IComicPanel _currentComicPanel;
    private IComicPanel _displayedPanel;
    private IComicPanel _replayingPanel; // panel currently being replayed (null when not replaying)
    private bool _isReplaying;
    private LoopCount _currentLoopCount;
    private readonly CommandHistory _commandHistory = new();

    /// <summary>
    ///     Relayed from the current panel's OnReadyForInput.
    ///     Fires after intro animation and after each blocking step completes.
    ///     NavigationController subscribes to unblock advance button input.
    ///     NavigationPresenter subscribes to show the "press spacebar" hint.
    ///     The bool argument is true when the hint should be shown — false for click-driven steps.
    /// </summary>
    public UnityEvent<bool> OnCurrentPanelReadyForInput { get; } = new();

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

    /// <summary>
    ///     Fired whenever the displayed panel changes — forward (SwitchToPanel), back (RetreatComic),
    ///     and forward-through-history (RedoPanel). MusicController subscribes to this to crossfade
    ///     to the new panel's MusicTrackSO. Consumers must not assume the panel is the story-front
    ///     panel — it may be a historical panel during back/forward navigation.
    /// </summary>
    public UnityEvent<IComicPanel> OnDisplayedPanelChanged { get; } = new();

    #endregion

    #region Methods

    private void Start()
    {
        // Discover all ComicPanel components in the scene automatically.
        // Panels register themselves by being present in the scene hierarchy — no Inspector wiring needed.
        // This avoids null slots caused by merge conflicts when teammates each add panels to Main.unity.
        ComicPanel[] found = FindObjectsByType<ComicPanel>(FindObjectsSortMode.None);
        _comicPanels = new IComicPanel[found.Length];
        for (int i = 0; i < found.Length; i++)
            _comicPanels[i] = found[i];

        if (_comicPanels.Length == 0)
            Debug.LogError("[ComicManager] No ComicPanel components found in the scene.", this);
    }

    /// <summary>
    ///     Starts the comic from the first eligible panel.
    ///     Called by TitleScreenController when the player presses the start button.
    ///     Panel 1's IncomingBlend (set on its PanelDataSO) controls the camera transition
    ///     from the title camera — set it to EaseInOut for a cinematic tilt-down, or Cut for an instant jump.
    /// </summary>
    public void StartComic()
    {
        IComicPanel first = PanelSelector.NextPanel(_comicPanels, null, _currentLoopCount);
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
    ///     During normal play: blocked when browsing history (CanRedo = true).
    ///     During replay: advances the displayed panel regardless of history position.
    /// </summary>
    public void AdvanceComic()
    {
        if (_isReplaying)
            _displayedPanel?.Advance();
        else if (!_commandHistory.CanRedo)
            _currentComicPanel.Advance();
        // else: browsing history and not replaying — advance button does nothing
    }

    /// <summary>
    ///     Called by back button (NavigationController.RetreatComic()).
    ///     Cancels any in-progress replay before navigating.
    /// </summary>
    public void RetreatComic()
    {
        CancelReplay();
        ICommand cmd = _commandHistory.Undo();
        if (cmd is not SwitchCameraCommand switchCmd) return;
        _displayedPanel = switchCmd.PanelAfterUndo;
        OnReplayAvailabilityChanged.Invoke(true);
        OnDisplayedPanelChanged.Invoke(_displayedPanel);
        BroadcastNavigationAvailability();
    }

    /// <summary>
    ///     Called by forward button (NavigationController.NextPanel()).
    ///     Cancels any in-progress replay before navigating.
    /// </summary>
    public void RedoPanel()
    {
        CancelReplay();
        ICommand cmd = _commandHistory.Redo();
        if (cmd is not SwitchCameraCommand switchCmd) return;
        _displayedPanel = switchCmd.PanelAfterExecute;
        OnReplayAvailabilityChanged.Invoke(true);
        OnDisplayedPanelChanged.Invoke(_displayedPanel);
        BroadcastNavigationAvailability();

        // If there's nothing left to redo we're back at the story front.
        // ShowInstant() never fires OnReadyForInput, so fire it here to show the advance hint.
        // All steps are done (panel is in its final state) — hint always shows to advance to next panel.
        if (!_commandHistory.CanRedo)
            OnCurrentPanelReadyForInput.Invoke(true);
    }

    /// <summary>
    ///     Called by replay button. Wires a temporary relay so the displayed panel's
    ///     OnReadyForInput unblocks the advance button (normally only the story-front panel
    ///     has this relay). Also wires OnPanelComplete to clean up when replay ends.
    /// </summary>
    public void ReplayCurrentPanel()
    {
        if (_displayedPanel == null) return;
        _replayingPanel = _displayedPanel;
        _isReplaying = true;

        // Historical panels aren't wired to OnCurrentPanelReadyForInput — add a temporary relay.
        // Story-front panel already has this relay via RewireCurrentPanel; skip to avoid double-fire.
        if (!ReferenceEquals(_replayingPanel, _currentComicPanel))
            _replayingPanel.OnReadyForInput.AddListener(OnCurrentPanelReadyForInput.Invoke);

        _replayingPanel.OnPanelComplete.AddListener(OnReplayComplete);
        _replayingPanel.Replay();
        OnReplayAvailabilityChanged.Invoke(false);
    }

    /// <summary>
    ///     Called when the replaying panel fires OnPanelComplete.
    ///     Cleans up replay state. For historical panels, fires OnCurrentPanelReadyForInput
    ///     to unblock the advance button (the presenter won't show the hint since
    ///     _advanceStructurallyAvailable is false for non-story-front panels).
    /// </summary>
    private void OnReplayComplete()
    {
        bool wasHistorical = !ReferenceEquals(_replayingPanel, _currentComicPanel);

        if (wasHistorical)
            _replayingPanel.OnReadyForInput.RemoveListener(OnCurrentPanelReadyForInput.Invoke);

        _replayingPanel.OnPanelComplete.RemoveListener(OnReplayComplete);
        _replayingPanel = null;
        _isReplaying = false;

        OnReplayAvailabilityChanged.Invoke(true); // Replay button available again.
        if (wasHistorical)
            OnCurrentPanelReadyForInput.Invoke(false); // Unblock advance button; hint suppressed (browsing history).
    }

    /// <summary>
    ///     Removes temporary replay listeners and clears replay state.
    ///     Called when history navigation interrupts an in-progress replay.
    /// </summary>
    private void CancelReplay()
    {
        if (!_isReplaying || _replayingPanel == null) return;

        if (!ReferenceEquals(_replayingPanel, _currentComicPanel))
            _replayingPanel.OnReadyForInput.RemoveListener(OnCurrentPanelReadyForInput.Invoke);

        _replayingPanel.OnPanelComplete.RemoveListener(OnReplayComplete);
        _replayingPanel = null;
        _isReplaying = false;
    }

    /// <summary>
    ///     Moves the OnPanelComplete and OnReadyForInput subscriptions from whatever panel
    ///     was current to <paramref name="next" />.
    /// </summary>
    private void RewireCurrentPanel(IComicPanel next)
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
    private void SwitchToPanel(IComicPanel next)
    {
        IComicPanel prev = _currentComicPanel;
        RewireCurrentPanel(next);

        // Always use the target panel's configured incoming blend.
        // On first launch this blends from the title camera's position — set Panel 1's IncomingBlend
        // to EaseInOut for a cinematic tilt-down, or Cut if no title camera is present.
        CinemachineBlendDefinition blend = next.IncomingBlend;

        // Capture whether this panel was already seen BEFORE Show() runs —
        // Show() sets HasBeenVisited = true on first visit, so we must read it first.
        bool isRevisit = next.HasBeenVisited;

        _commandHistory.Execute(new SwitchCameraCommand(prev, next, brain, blend));
        _displayedPanel = next;
        OnReplayAvailabilityChanged.Invoke(isRevisit);
        OnDisplayedPanelChanged.Invoke(_displayedPanel);
        BroadcastNavigationAvailability();
    }

    /// <summary>
    ///     Subscribed to the current panel's OnPanelComplete event.
    ///     Selects the next panel, advances the loop count if wrapping, and switches to it.
    /// </summary>
    private void MoveToNextPanel()
    {
        IComicPanel next = PanelSelector.NextPanel(_comicPanels, _currentComicPanel, _currentLoopCount);

        if (next == null)
        {
            Debug.LogWarning("[ComicManager] No eligible panels found. Check panel data and loop count.", this);
            return;
        }

        // A wrap-around means the current panel was the last in this loop — advance the loop count.
        bool wrapped = next.Rank <= _currentComicPanel.Rank;
        if (wrapped && _currentLoopCount < LoopCountBounds.Last)
        {
            _currentLoopCount++;
            // Re-query with the incremented loop count so newly-unlocked panels are included.
            next = PanelSelector.NextPanel(_comicPanels, null, _currentLoopCount);
            if (next == null)            {
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

    //Loop Count Getter Function
    public LoopCount GetLoopCount()
    {
        return _currentLoopCount;
    }

    #endregion
}