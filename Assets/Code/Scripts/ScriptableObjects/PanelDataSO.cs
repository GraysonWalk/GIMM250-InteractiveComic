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
    [Header("Loop Eligibility")]
    [SerializeField] private LoopCount firstLoop;
    [Tooltip("The last loop this panel appears in. Defaults to Loop3 (always visible). " +
             "Set lower to retire a panel after a specific loop.")]
    [SerializeField] private LoopCount lastLoop = LoopCountBounds.Last;
    [SerializeField] private int rank;

    [Header("Completion")]
    [Tooltip("If ticked, the panel advances to its first step automatically when the intro animation " +
             "finishes — no button press needed. Use for purely cinematic panels with no interaction.")]
    [SerializeField] private bool autoAdvanceAfterIntro;

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

    [Tooltip("Duration in seconds for the intro animation cross-fade when this panel is shown. " +
             "Shorter values snap to the intro faster; longer values give a softer start.")]
    [SerializeField] private float introCrossFadeDuration = 0.1f;

    [Header("Audio")]
    [Tooltip("Music to play when this panel is displayed. Leave empty to keep the current music " +
             "playing unchanged. Assign a MusicTrackSO with no clip to fade music out to silence.")]
    [SerializeField] private MusicTrackSO music;

    public int Rank => rank;
    public LoopCount FirstLoop => firstLoop;
    public LoopCount LastLoop => lastLoop;
    public bool AutoAdvanceAfterIntro => autoAdvanceAfterIntro;
    public bool RequireAdvanceToComplete => requireAdvanceToComplete;
    public bool ReplayAnimationOnRevisit => replayAnimationOnRevisit;
    public CinemachineBlendDefinition IncomingBlend => incomingBlend;
    public float IntroCrossFadeDuration => introCrossFadeDuration;
    public MusicTrackSO Music => music;
}