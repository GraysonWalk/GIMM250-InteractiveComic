using UnityEngine;

namespace StateMachine
{
    /// <summary>
    /// An abstract base class to enforce that certain parameters are passed into the constructor of every state.
    /// </summary>
    public abstract class BaseState : IState
    {
        #region Variables

        /*
         * Enforce parameters for all states
         * Set as protected because only inheriting classes need access
         * Set as readonly because they should not change after being set in constructor
         */
        protected readonly Animator Animator; // Reference to the animator component
        
        // Hashes for animator parameters to optimize performance by avoiding string lookups
        protected static readonly int IntroHash = Animator.StringToHash("Intro"); // Turns 'Intro' into an int hash
        protected static readonly int ExitHash = Animator.StringToHash("Exit"); // Turns 'Exit' into an int hash
        
        protected const float CrossFadeDuration = 0.1f; // Duration for animation crossfades, set as constant since it won't change

        #endregion

        #region Constructor

        /// <summary>
        /// Constructor to enforce parameters for all states
        /// </summary>
        /// <param name="animator">The animator</param>
        protected BaseState(Animator animator)
        {
            // Assign passed in parameters
            this.Animator = animator;
        }

        #endregion

        #region Methods

        public virtual void OnEnter()
        {
            // noop
        }

        public virtual void Update()
        {
            // noop
        }

        public virtual void FixedUpdate()
        {
            // noop
        }

        public virtual void OnExit()
        {
            // noop
        }

        #endregion
    }
}