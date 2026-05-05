using Unity.Cinemachine;
using UnityEngine.Events;

public interface IComicPanel
{
    int Rank { get; }
    LoopCount FirstLoop { get; }
    LoopCount LastLoop { get; }
    CinemachineBlendDefinition IncomingBlend { get; }

    /// <summary>
    ///     Music to play when this panel is displayed. Null = no change (keep the current track playing).
    ///     A non-null SO with a null Clip = fade current music to silence. Read by MusicController.
    /// </summary>
    MusicTrackSO Music { get; }

    UnityEvent OnPanelComplete { get; }

    /// <summary>
    ///     Fired when the panel is ready for the next advance button press:
    ///     after intro animation, after each non-blocking step, and after each blocking step completes.
    ///     The bool argument is true when the advance hint ("press spacebar") should be shown —
    ///     false when the next step is click-driven (FocusPoint, MiniGame) and the hint is misleading.
    /// </summary>
    UnityEvent<bool> OnReadyForInput { get; }

    bool HasBeenVisited { get; } // True after the first Show() call; used to decide whether to offer replay

    void Show(); // Show with entry animation — used by forward Advance
    void ShowInstant(); // Show persistent steps only, no animation — used by history (UI arrows)
    void Replay(); // Replay all replayable steps from the start — used by the Replay button
    void Hide();
    void Advance();
}