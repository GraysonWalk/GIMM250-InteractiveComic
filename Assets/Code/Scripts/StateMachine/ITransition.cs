namespace StateMachine
{
    /// <summary>
    /// Interface for transitions between states in a state machine.
    /// Transitions will have a target state and a condition (predicate) that must be met to trigger the transition.
    /// </summary>
    public interface ITransition
    {
        IState TargetState { get; } // The state to transition to
        IPredicate Condition { get; } // The condition that must be met to trigger the transition
    }
}