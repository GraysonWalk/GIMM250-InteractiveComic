using UnityEngine;

/// <summary>
///     Manages the title screen of the game, including displaying the title, handling user input to start the game, and
///     transitioning to the main game scene.
/// </summary>
/// TODO: Implement title UI display and start button logic.
/// TODO: Change Start() in ComicManager to public StartComic() method to be controlled form here.
public class TitleScreenController : MonoBehaviour
{
}

/// <summary>
///     Manages the end screen of the game, including displaying the end message, handling user input to restart or quit,
///     and transitioning back to the title screen or exiting the game.
/// </summary>
/// TODO: In ComicManager add OnComicComplete
/// TODO: In ComicManager fire OnComicComplete when the last loop is done
/// TODO: Subscribe to OnComicComplete in OnEnable()
public class EndScreenController : MonoBehaviour
{
}