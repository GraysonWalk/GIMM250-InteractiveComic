using UnityEngine;
using UnityEngine.Events;

/// <summary>
///     Shared base for all IPanelStep implementations.
///     Owns the activation/replay state that every step type needs:
///     whether input blocks after activation (IsBlocking), the revisit behaviour flags,
///     the completion event, and Deactivate().
///     Subclasses implement Activate() and ShowInFinalState for their specific behaviour.
///
///     Loop-revisit behaviour (replayOnRevisit / hideOnRevisit) is controlled via BeginActivation().
///     Explicit replay (Replay button) always resets all steps via PrepareForReplay() —
///     override PrepareForReplay() in a subclass to preserve state across replays (e.g. FocusPoint).
/// </summary>
public abstract class StepBase : MonoBehaviour, IPanelStep
{
    #region Variables

    [Tooltip("Controls what happens when the player reaches this panel again in a later loop " +
             "(not the Replay button — that always replays all steps). " +
             "Tick to replay the animation; untick to show frozen or hidden instead.")]
    [SerializeField] protected bool replayOnRevisit;

    [Tooltip("If ticked, this step stays hidden on subsequent loop visits to the panel. " +
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

    /// <summary>Hides this step's GameObject. Override to hide additional objects (e.g. a separate presenter).</summary>
    public virtual void Deactivate() => gameObject.SetActive(false);

    /// <summary>
    ///     Resets this step so it animates and blocks again on the next Advance() call.
    ///     Called by ComicPanel.Replay() before the panel restarts.
    ///     Override in subclasses that should preserve their state across explicit replays
    ///     (e.g. FocusPoint — shows the previously chosen option rather than re-presenting the choice).
    /// </summary>
    public virtual void PrepareForReplay()
    {
        _hasBeenActivated = false;
    }

    /// <summary>
    ///     Records that Activate() has been called and returns whether this activation should skip
    ///     (i.e. the step has already run and replayOnRevisit is false — loop-revisit path only).
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
