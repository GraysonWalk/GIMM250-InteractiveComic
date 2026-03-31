using System;
using System.Collections.Generic;

namespace StateMachine
{
    /// <summary>
    /// A finite state machine class that manages states and transitions between them.
    /// It supports adding states, defining transitions with conditions, and updating the current states
    /// </summary>
    public class StateMachine
    {
        #region Variables
        
        // The current state of the state machine, represented as a StateNode
        StateNode _currentState;

        // A dictionary to map state types (System.Type) to StateNode instances
        private Dictionary<Type, StateNode> _nodes = new(); 

        // A hash set for 'any' transitions that can be triggered any time without needing a 'from' state
        HashSet<ITransition> _anyTransitions = new(); 
        
        #endregion
        
        #region Unity Methods

        public void Update()
        {
            var transition = GetTransition(); // Check for any valid transition from the current state
            if (transition != null) // If a valid transition is found, change to the target state
                ChangeState(transition.TargetState);

            _currentState.State?.Update(); // Call the Update method of the current state if it exists
        }

        public void FixedUpdate()
        {
            _currentState.State?.FixedUpdate(); // Call the FixedUpdate method of the current state if it exists
        }

        /// <summary>
        /// Allows for setting the state of the state machine from outside the class.
        /// </summary>
        /// <param name="state">The state to set</param>
        public void SetState(IState state)
        {
            _currentState = _nodes[state.GetType()]; // Set the current state by looking it up in the dictionary by type
            _currentState.State.OnEnter(); // Call the OnEnter method of the new current state
        }
        
        #endregion
        
        #region Custom Methods

        /// <summary>
        /// Changes the current state to a new state, handling exit and enter calls.
        /// </summary>
        /// <param name="state">The state to transition to</param>
        void ChangeState(IState state)
        {
            if (state == _currentState.State) return; // If attempting to change to the same state, do nothing

            var previousState =
                _currentState.State; // Set the value of previousState to the current state before changing
            var nextState = _nodes[state.GetType()].State; // Look up the next state in the dictionary by type

            previousState?.OnExit(); // Call OnExit on the previous state if it exists
            nextState?.OnEnter(); // Call OnEnter on the next state if it exists
            _currentState =
                _nodes
                    [state.GetType()]; // Make sure the current state is set to the actual StateNode stored in the dictionary
        }

        /// <summary>
        /// Checks for any valid transition from the current state, prioritizing 'any' transitions.
        /// Returns the first valid transition found, or null if none are valid.
        /// </summary>
        /// <returns></returns>
        ITransition GetTransition()
        {
            foreach (var transition in
                     _anyTransitions) // Iterate over 'any' transitions before checking state-specific transitions
                if (transition.Condition.Evaluate())
                    return transition; // If a transition condition is met (evaluates to true), return that transition

            foreach (var transition in
                     _currentState.Transitions) // Iterate over transitions specific to the current state
                if (transition.Condition.Evaluate())
                    return transition; // If a transition condition is met, return that transition

            return null; // No valid transition found
        }

        /// <summary>
        /// Adds a transition from one state to another with a specified condition.
        /// If the state type does not already exist in the dictionary, a StateNode will be created and added.
        /// </summary>
        /// <param name="fromState">Origin State</param>
        /// <param name="toState">Destination State</param>
        /// <param name="condition">Transition Condition</param>
        public void AddTransition(IState fromState, IState toState, IPredicate condition)
        {
            GetOrAddNode(fromState)
                .AddTransition(GetOrAddNode(toState).State,
                    condition); // Add a transition from 'fromState' to 'toState' with the given condition
        }

        /// <summary>
        /// Adds a transition that can occur from any state to the specified state when the condition is met.
        /// If the state type does not already exist in the dictionary, a StateNode will be created and added.
        /// </summary>
        /// <param name="toState"></param>
        /// <param name="condition"></param>
        public void AddAnyTransition(IState toState, IPredicate condition)
        {
            _anyTransitions.Add(new Transition(GetOrAddNode(toState).State,
                condition)); // Add a transition to the 'any' transitions set
        }

        /// <summary>
        /// Returns the StateNode for the given state from the 'nodes' dictionary.
        /// If the state type does not exist in the dictionary, a new StateNode will be created and added.
        /// </summary>
        /// <param name="state">State to look up</param>
        /// <returns></returns>
        StateNode GetOrAddNode(IState state)
        {
            var node = _nodes.GetValueOrDefault(state
                .GetType()); // Try to get the StateNode from the dictionary by the state's type

            if (node == null) // If the state type does not exist in the dictionary, create a new StateNode and add it
            {
                node = new StateNode(state);
                _nodes.Add(state.GetType(), node);
            }

            return node; // Return the existing or newly created StateNode
        }
        
        #endregion
        
        #region Internal StateNode Class
        
        /// <summary>
        /// An internal class to represent a state and its associated transitions.
        /// Can be private because nothing else needs to know about it (encapsulation).
        /// </summary>
        class StateNode
        {
            public IState State { get; } // The state represented by this node
            
            // A set of transitions for this state, using a HashSet to avoid duplicates and allow fast lookups
            public HashSet<ITransition> Transitions { get; }

            /// <summary>
            /// A constructor to create a StateNode, it accepts a state and initializes an empty set of transitions.
            /// </summary>
            /// <param name="state">State to be assigned to state node</param>
            public StateNode(IState state)
            {
                State = state; // Assign the state
                Transitions = new HashSet<ITransition>(); // Initialize an empty set of transitions
            }

            /// <summary>
            /// A method to add transitions as the StateMachine is being configured.
            /// </summary>
            /// <param name="targetState">State being moved to</param>
            /// <param name="condition">Condition on which moving to state</param>
            public void AddTransition(IState targetState, IPredicate condition)
            {
                Transitions.Add(new Transition(targetState, condition)); // Add a new transition to the set
            }
        }
        
        #endregion
    }
}

/*
 * References:
 * Build a Better Finite State Machine by Git-Amend: https://www.youtube.com/watch?v=NnH6ZK5jt7o
 */