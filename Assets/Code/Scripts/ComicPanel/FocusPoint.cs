using UnityEngine;
using UnityEngine.Events;

/// <summary>
///     Represents a focus point in the comic panel where the player can make a choice. Each focus point
///     presents two named options for one thematic category and writes the result to PlayerChoicesSO.
/// </summary>
/// TODO: Turn into IPanelStep
/// TODO: Add [SerializeField] fields for PlayerChoicesSO and the choice category
/// TODO: Add logic to show choice buttons and wait
/// TODO: Add ability to set focus on PlayerChoicesSO via SetScienceFocus / SetPhilosophyFocus / SetLeadershipFocus
public class FocusPoint : MonoBehaviour
{
    private ScienceChoice    _scienceChoice;
    private PhilosophyChoice _philosophyChoice;
    private LeadershipChoice _leadershipChoice;
    private UnityEvent       _onFocusSelected;
}