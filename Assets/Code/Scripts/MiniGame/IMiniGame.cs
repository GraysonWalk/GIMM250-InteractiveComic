using UnityEngine.Events;

/// <summary>
///     Interface for the game logic of a minigame.
///     Implement this on a MonoBehaviour alongside MiniGame (which handles IPanelStep).
///     Concrete minigames override StartGame() and call Complete() or Fail() when done.
/// </summary>
public interface IMiniGame
{
    /// <summary>Called when the minigame becomes active. Override to set up game state.</summary>
    void StartGame();

    /// <summary>Called when the minigame ends for any reason. Fires OnStepComplete.</summary>
    void EndGame();

    /// <summary>Fired when the player successfully completes the minigame.</summary>
    UnityEvent OnGameComplete { get; }

    /// <summary>Fired when the player fails the minigame.</summary>
    UnityEvent OnGameFailed { get; }
}
