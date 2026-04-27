using System.Collections.Generic;

/// <summary>
///     Manages the history of commands, allowing undo and redo.
///     Execute() records a command and clears any redo-able future (branching replaces old branch).
///     Undo() reverses the last command and makes it available to redo.
///     Redo() re-applies a previously undone command using ShowInstant (no animation).
///     CanUndo / CanRedo expose stack state so NavigationPresenter can enable/disable UI arrows.
/// </summary>
public class CommandHistory
{
    #region Variables

    private readonly Stack<ICommand> _history = new();
    private readonly Stack<ICommand> _future = new();

    // > 1 so the first command (null → Panel1) stays as an anchor and is never popped.
    // Undoing it would leave no active camera (PreviousPanel is null on the first command).
    public bool CanUndo => _history.Count > 1;
    public bool CanRedo => _future.Count > 0;

    #endregion

    #region Methods

    /// <summary>Executes a command, records it, and discards any undone future branch.</summary>
    public void Execute(ICommand command)
    {
        command.Execute();
        _history.Push(command);
        _future.Clear(); // New branch — previously undone commands are no longer reachable
    }

    /// <summary>Undoes the most recent command and makes it available to redo.</summary>
    /// <returns>The command that was undone, so callers can inspect what changed.</returns>
    public ICommand Undo()
    {
        if (!CanUndo) return null;
        ICommand command = _history.Pop();
        command.Undo();
        _future.Push(command);
        return command;
    }

    /// <summary>
    ///     Re-applies the most recently undone command. Calls Redo() on the command,
    ///     which uses ShowInstant() so no animation replays.
    /// </summary>
    /// <returns>The command that was redone, so callers can inspect what changed.</returns>
    public ICommand Redo()
    {
        if (!CanRedo) return null;
        ICommand command = _future.Pop();
        command.Redo();
        _history.Push(command);
        return command;
    }

    #endregion
}