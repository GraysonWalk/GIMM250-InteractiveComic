using Unity.Cinemachine;

/// <summary>
///     Command to switch the camera to a different panel. This command will handle the transition between panels,
///     including any necessary animations or effects. It will also allow for undoing and redoing the camera switch,
///     so that the player can easily navigate back and forth between panels.
/// </summary>
public class SwitchCameraCommand : ICommand
{
    #region Constructor

    /// <param name="previousPanel">The panel being left (null on first launch).</param>
    /// <param name="targetPanel">The panel being entered.</param>
    /// <param name="brain">The CinemachineBrain that controls blending.</param>
    /// <param name="blend">The blend style to use for this forward transition.</param>
    public SwitchCameraCommand(IComicPanel previousPanel, IComicPanel targetPanel,
        CinemachineBrain brain, CinemachineBlendDefinition blend)
    {
        PanelAfterUndo = previousPanel;
        PanelAfterExecute = targetPanel;
        _brain = brain;
        _blend = blend;
    }

    #endregion

    #region Variables

    private readonly CinemachineBrain _brain;
    private readonly CinemachineBlendDefinition _blend;

    // Used when there is no destination panel (e.g. PanelAfterUndo is null on the first command).
    private static readonly CinemachineBlendDefinition InstantCut =
        new(CinemachineBlendDefinition.Styles.Cut, 0f);

    /// <summary>The panel displayed after Execute() or Redo() — the target panel.</summary>
    public IComicPanel PanelAfterExecute { get; }

    /// <summary>The panel displayed after Undo() — the previous panel (null on the first command).</summary>
    public IComicPanel PanelAfterUndo { get; }

    #endregion

    #region Methods

    /// <summary>Forward story progression — uses this panel's configured blend and plays the full animation.</summary>
    public void Execute()
    {
        if (_brain != null) _brain.DefaultBlend = _blend;
        PanelAfterUndo?.Hide();
        PanelAfterExecute.Show();
    }

    /// <summary>History navigation backward — blends to the previous panel's end state.</summary>
    public void Undo()
    {
        if (_brain != null)
            _brain.DefaultBlend = PanelAfterUndo != null ? PanelAfterUndo.IncomingBlend : InstantCut;
        PanelAfterExecute.Hide();
        PanelAfterUndo?.ShowInstant();
    }

    /// <summary>History navigation forward — blends to the target panel's end state.</summary>
    public void Redo()
    {
        if (_brain != null)
            _brain.DefaultBlend = PanelAfterExecute.IncomingBlend;
        PanelAfterUndo?.Hide();
        PanelAfterExecute.ShowInstant();
    }

    #endregion
}