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
    void ShowInstant(); // Show persistent dialogue points only, no animation — used by history (UI arrows)

    void
        Hide(); // Hide with exit animation — used by forward Advance when the next panel's intro overlaps the current panel's outro

    void
        Advance(); // Advance to the next step within the panel, or fire OnPanelComplete if there are no more steps. Plays animations and blocks input until each step's OnAnimationFinished event.
}