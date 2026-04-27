using UnityEngine;

/// TODO: Reconfigure the choice enum structure.
/// TODO: Add SetFoces method that writes chosen values
/// TODO: Add choice reset for replay at end
[CreateAssetMenu(fileName = "PlayerChoicesSO", menuName = "Comic/Player Choices", order = 0)]
public class PlayerChoicesSO : ScriptableObject
{
    [SerializeField] private FocusOption leadershipFocus;
    [SerializeField] private FocusOption philosophyFocus;
    [SerializeField] private FocusOption scienceFocus;
    public FocusOption LeadershipFocus => leadershipFocus;
    public FocusOption PhilosophyFocus => philosophyFocus;
    public FocusOption ScienceFocus => scienceFocus;
}