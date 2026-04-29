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
    ///     Steps that should replay (replayOnRevisit = true) reset their visited state here
    ///     so they block input and animate again. Steps that should not replay (e.g. FocusPoint)
    ///     leave their state untouched so they remain non-blocking on the next pass.
    /// </summary>
    void PrepareForReplay();
}