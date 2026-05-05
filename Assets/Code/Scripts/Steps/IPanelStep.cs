using UnityEngine.Events;

/// <summary>
///     Represents a single step in a panel's advance sequence — anything that can be
///     triggered by the player pressing the advance button.
///     Both AnimatedStep and MiniGame implement this interface.
///     ComicPanel.Advance() activates steps in hierarchy order, one per advance button press.
///     If IsBlocking is true, Advance() is locked until OnStepComplete fires.
///     ComicPanel.ShowInstant() calls Activate() on persistent steps and Deactivate() on others.
/// </summary>
public interface IPanelStep
{
    /// <summary>If true, Advance() is locked after Activate() until OnStepComplete fires.</summary>
    bool IsBlocking { get; }

    /// <summary>
    ///     If true, the "press spacebar" advance hint should be shown when this step is next in
    ///     the queue. Set to false on click-driven steps (FocusPoint, MiniGame) so the hint does
    ///     not appear while the player is expected to interact with the panel directly.
    /// </summary>
    bool ShowAdvanceHint { get; }

    /// <summary>
    ///     True when this step should be shown by ShowInstant() (history navigation / final state).
    ///     Implementations combine designer intent (e.g. persistsInFinalState) with runtime state
    ///     (e.g. hasBeenActivated) — ShowInstant() asks one question and gets one answer.
    /// </summary>
    bool ShowInFinalState { get; }

    /// <summary>Fired when a blocking step finishes. Non-blocking steps never need to fire this.</summary>
    UnityEvent OnStepComplete { get; }

    /// <summary>Triggered when the player presses the advance button on this step's turn.</summary>
    void Activate(PlayerChoicesSO choices);

    /// <summary>Hide or reset this step (called on Show() reset and for non-persistent steps in ShowInstant()).</summary>
    void Deactivate();

    /// <summary>
    ///     Called by ComicPanel.Replay() before the panel restarts.
    ///     Resets the step so it blocks input and animates again on the next Advance() call.
    ///     Steps that should preserve state across explicit replays (e.g. FocusPoint showing
    ///     the previously chosen option) override this in their concrete class.
    /// </summary>
    void PrepareForReplay();
}