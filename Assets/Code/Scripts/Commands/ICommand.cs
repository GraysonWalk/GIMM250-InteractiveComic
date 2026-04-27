public interface ICommand
{
    void Execute();

    void Undo();

    // Default: redo behaves the same as execute.
    // Override only when redo requires different behavior (e.g. SwitchCameraCommand uses ShowInstant).
    void Redo()
    {
        Execute();
    }
}