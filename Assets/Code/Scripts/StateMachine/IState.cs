namespace StateMachine
{
    /// <summary>
    ///  The interface for states in state machines, enforcing methods for entering, updating, and exiting states.
    /// </summary>
    public interface IState
    {
        void OnEnter(); // Called when the state is entered
        void Update();
        void FixedUpdate();
        void OnExit(); // Called when the state is exited
    }
}