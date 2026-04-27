using Unity.Cinemachine;
using UnityEngine.Events;

public interface IComicPanel
{
    int Rank { get; }
    LoopCount FirstLoop { get; }
    CinemachineBlendDefinition IncomingBlend { get; }
    UnityEvent OnPanelComplete { get; }

    /// <summary>
    ///     Fired when the panel is ready for the next advance button press:
    ///     after intro animation, after each non-blocking step, and after each blocking step completes.
    /// </summary>
    UnityEvent OnReadyForInput { get; }

    bool HasBeenVisited { get; } // True after the first Show() call; used to decide whether to offer replay

    void Show(); // Show with entry animation — used by forward Advance
    void ShowInstant(); // Show persistent steps only, no animation — used by history (UI arrows)
    void Replay(); // Replay all replayable steps from the start — used by the Replay button
    void Hide();
    void Advance();
}