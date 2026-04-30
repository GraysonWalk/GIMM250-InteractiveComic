using UnityEngine;

/// <summary>
///     Shared runtime state for all three player focus choices. A single asset is referenced
///     by every panel via [SerializeField] — no class should read choices through ComicManager.
///     Choices persist across panels within a session. Call ResetChoices() at the start of a
///     new playthrough.
/// </summary>
[CreateAssetMenu(fileName = "PlayerChoicesSO", menuName = "Comic/Player Choices", order = 0)]
public class PlayerChoicesSO : ScriptableObject
{
    [SerializeField] private ScienceChoice scienceFocus;
    [SerializeField] private PhilosophyChoice philosophyFocus;
    [SerializeField] private LeadershipChoice leadershipFocus;

    public ScienceChoice ScienceFocus => scienceFocus;
    public PhilosophyChoice PhilosophyFocus => philosophyFocus;
    public LeadershipChoice LeadershipFocus => leadershipFocus;

    public void SetScienceFocus(ScienceChoice choice)    => scienceFocus    = choice;
    public void SetPhilosophyFocus(PhilosophyChoice choice) => philosophyFocus = choice;
    public void SetLeadershipFocus(LeadershipChoice choice) => leadershipFocus = choice;

    /// <summary>Resets all three focus choices to None. Call at the start of a new playthrough.</summary>
    public void ResetChoices()
    {
        scienceFocus    = ScienceChoice.None;
        philosophyFocus = PhilosophyChoice.None;
        leadershipFocus = LeadershipChoice.None;
    }
}