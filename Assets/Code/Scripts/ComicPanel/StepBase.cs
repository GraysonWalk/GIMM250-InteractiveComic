using UnityEngine;
using UnityEngine.Events;

/// <summary>
///     Shared base for all IPanelStep implementations.
///     Owns the activation/replay state that every step type needs:
///     whether input blocks after activation (IsBlocking), the revisit behavior flags,
///     the completion event, and Deactivate().
///     Subclasses implement Activate() and ShowInFinalState for their specific behavior.
/// </summary>
public abstract class StepBase : MonoBehaviour, IPanelStep
{
    #region Variables

    [Tooltip("If ticked, this step replays its animation/logic when the panel is revisited or replayed. " +
             "Untick for steps that should not repeat once completed (e.g. a choice already answered).")]
    [SerializeField] protected bool replayOnRevisit;

    [Tooltip("If ticked, this step stays hidden on subsequent visits to the panel. " +
             "Only meaningful when Replay On Revisit is unticked. " +
             "Use for steps whose content was relevant only on first view.")]
    [SerializeField] protected bool hideOnRevisit;

    private bool _hasBeenActivated;

    /// <summary>Read-only access to activation state for subclasses (e.g. ShowInFinalState guards).</summary>
    protected bool HasBeenActivated => _hasBeenActivated;

    public bool IsBlocking => !_hasBeenActivated || replayOnRevisit;
    public abstract bool ShowInFinalState { get; }
    public UnityEvent OnStepComplete { get; } = new();

    #endregion

    #region Methods

    public abstract void Activate(PlayerChoicesSO choices);
    
    public void Deactivate() => gameObject.SetActive(false);

    /// <summary>
    ///     Resets activation state if this step is configured to replay,
    ///     so it blocks input and runs again on the next panel replay.
    /// </summary>
    public void PrepareForReplay()
    {
        if (replayOnRevisit)
            _hasBeenActivated = false;
    }

    /// <summary>
    ///     Records that Activate() has been called and returns whether this activation should skip
    ///     (i.e. the step has already run and replayOnRevisit is false).
    ///     Always call this at the start of Activate() — it also sets HasBeenActivated.
    /// </summary>
    protected bool BeginActivation()
    {
        bool skip = _hasBeenActivated && !replayOnRevisit;
        _hasBeenActivated = true;
        return skip;
    }

    #endregion
}
