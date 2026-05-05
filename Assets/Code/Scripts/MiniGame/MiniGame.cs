using UnityEngine.Events;

/// <summary>
///     Base class for all minigames. Place as a child GameObject of a ComicPanel.
///     Implements IPanelStep (via StepBase) so ComicPanel.Advance() activates it like any other step.
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
public class MiniGame : StepBase, IMiniGame
{
    #region Variables

    public UnityEvent OnGameComplete { get; } = new();
    public UnityEvent OnGameFailed   { get; } = new();

    // Minigames are interactive — they never persist as static display elements in the final state.
    public override bool ShowInFinalState => false;

    // Minigame interaction is game-driven (clicks, drags, etc.); the spacebar advance hint should
    // not appear while this step is queued — there is no "press spacebar" action for the player.
    public override bool ShowAdvanceHint => false;

    #endregion

    #region Methods

    /// <summary>Called by ComicPanel when this step's turn is reached. Shows the minigame and starts it.</summary>
    public override void Activate(PlayerChoicesSO choices)
    {
        bool skip = BeginActivation();
        if (skip) return; // Already completed and not set to replay — panel moves on without blocking.
        gameObject.SetActive(true);
        StartGame();
    }

    /// <summary>Override in subclasses to set up game state, spawn objects, start timers, etc.</summary>
    public virtual void StartGame() { }

    /// <summary>Called internally when the game ends. Fires OnStepComplete to unblock the panel.</summary>
    public virtual void EndGame()
    {
        OnStepComplete.Invoke();
        gameObject.SetActive(false);
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
        EndGame(); // Panel advances past the failed minigame; handle retry logic before calling Fail() if needed.
    }

    #endregion
}