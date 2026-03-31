namespace StateMachine
{
    /// <summary>
    /// Default transition implementation.
    /// Contains a target state and a condition (predicate) to evaluate for the transition.
    /// Uses a constructor and getters because these properties should be immutable after creation.
    /// </summary>
    public class Transition : ITransition
    {
        public IState TargetState { get; } // The state to transition to
        public IPredicate Condition { get; } // The condition that must be met to trigger the transition

        public Transition(IState targetState, IPredicate condition) // Constructor to set the target state and condition
        {
            TargetState = targetState;
            Condition = condition;
        }
    }
}