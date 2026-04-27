using Unity.Cinemachine;
using UnityEngine;

/// <summary>
///     Holds static configuration data for a comic panel, such as which loop it first appears in, which variants it has,
///     and how the camera transitions to it. This is separate from the dynamic content (dialogue, minigames) that lives on
///     child GameObjects of the ComicPanel. By using a ScriptableObject, we can easily create and edit panel
///     configurations in the Unity Editor without hardcoding values in scripts or prefabs.
/// </summary>
[CreateAssetMenu(fileName = "PanelDataSO", menuName = "Comic/Panel Data", order = 0)]
public class PanelDataSO : ScriptableObject
{
    [SerializeField] private LoopCount firstLoop;
    [SerializeField] private bool hasLeadershipVariant;
    [SerializeField] private bool hasPhilosophyVariant;
    [SerializeField] private bool hasScienceVariant;
    [SerializeField] private int rank;

    [Header("Completion")]
    [Tooltip("If ticked, the player must press the advance button after the last step before moving " +
             "to the next panel. Untick to advance automatically when the last step finishes.")]
    [SerializeField] private bool requireAdvanceToComplete = true;

    [Header("Loop Revisit")]
    [Tooltip("If ticked, the entry animation replays when this panel is revisited in a later loop. " +
             "Untick for panels whose content does not change between loops.")]
    [SerializeField] private bool replayAnimationOnRevisit = true;

    [Header("Camera Transition")]
    [Tooltip("How the camera blends INTO this panel. EaseInOut at 1s is the project default. " +
             "Override per panel for cuts, slow reveals, or snappy transitions.")]
    [SerializeField] private CinemachineBlendDefinition incomingBlend =
        new(CinemachineBlendDefinition.Styles.EaseInOut, 1f);

    public int Rank => rank;
    public LoopCount FirstLoop => firstLoop;
    public bool HasLeadershipVariant => hasLeadershipVariant;
    public bool HasPhilosophyVariant => hasPhilosophyVariant;
    public bool HasScienceVariant => hasScienceVariant;
    public bool RequireAdvanceToComplete => requireAdvanceToComplete;
    public bool ReplayAnimationOnRevisit => replayAnimationOnRevisit;
    public CinemachineBlendDefinition IncomingBlend => incomingBlend;
}