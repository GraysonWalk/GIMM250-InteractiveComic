using UnityEngine;
using UnityEngine.Events;

/// <summary>
///     Abstract base class for all minigames. Place as a child GameObject of a ComicPanel.
///     Implements IPanelStep so ComicPanel.Advance() can activate it like any other step.
///     Implements IMiniGame for the game logic contract.
///     Designer workflow:
///     1. Create a child GameObject under ComicPanel, attach a concrete MiniGame subclass.
///     2. Position it in the hierarchy where you want it to trigger during the advance sequence.
///     3. The minigame starts hidden; ComicPanel activates it at the right step automatically.
///     Developer workflow:
///     1. Create a new class that extends MiniGame.
///     2. Override StartGame() to set up your game logic.
///     3. Call Complete() when the player wins, or Fail() when the player loses.
/// </summary>
public abstract class MiniGame : MonoBehaviour, IPanelStep, IMiniGame
{
    #region Variables

    public UnityEvent OnGameComplete { get; } = new();
    public UnityEvent OnGameFailed { get; } = new();
    public UnityEvent OnStepComplete { get; } = new();

    [Tooltip("If ticked, this minigame runs again when the panel is replayed. " +
             "Leave unticked (default) so completed minigames are skipped on revisit.")]
    [SerializeField] private bool replayOnRevisit;

    private bool _hasBeenActivated;

    // Blocks input until EndGame() fires — unless already completed and replayOnRevisit is false.
    public bool IsBlocking => !_hasBeenActivated || replayOnRevisit;

    // Minigames never persist in the final state — they are interactive, not display elements.
    public bool PersistsInFinalState => false;

    #endregion

    #region Methods

    /// <summary>Called by ComicPanel when this step's turn is reached. Shows the minigame and starts it.</summary>
    public void Activate(PlayerChoicesSO choices)
    {
        bool skip = _hasBeenActivated && !replayOnRevisit;
        _hasBeenActivated = true;
        if (skip) return; // Already completed and not set to replay — panel moves on without blocking.
        gameObject.SetActive(true);
        StartGame();
    }

    /// <summary>Called by ComicPanel to hide this step (reset or non-persistent ShowInstant).</summary>
    public void Deactivate()
    {
        gameObject.SetActive(false);
    }

    /// <summary>Resets visited state if this minigame should replay on the next panel replay.</summary>
    public void PrepareForReplay()
    {
        if (replayOnRevisit)
            _hasBeenActivated = false;
    }

    /// <summary>Override to set up game state, spawn objects, start timers, etc.</summary>
    public virtual void StartGame()
    {
    }

    /// <summary>Called internally when the game ends. Fires OnStepComplete to unblock the panel.</summary>
    public virtual void EndGame()
    {
        gameObject.SetActive(false);
        OnStepComplete.Invoke();
    }

    /// <summary>Call this from your subclass when the player successfully completes the minigame.</summary>
    protected void Complete()
    {
        OnGameComplete.Invoke();
        EndGame();
    }

    /// <summary>Call this from your subclass when the player fails the minigame.</summary>
    protected void Fail()
    {
        OnGameFailed.Invoke();
        EndGame(); // Panel advances past the failed minigame; handle retry logic before calling Fail() if needed
    }

    #endregion
}