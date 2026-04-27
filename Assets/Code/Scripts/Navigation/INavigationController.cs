public interface INavigationController
{
    void Advance(); // Reference to Advance Input — step forward within panel or move to next panel, plays animations
    void NextPanel(); // Right UI arrow — jump forward in history instantly, no animation
    void PreviousPanel(); // Left UI arrow — jump back in history instantly, no animation
}