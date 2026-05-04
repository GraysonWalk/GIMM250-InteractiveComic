using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
///     Manages the title screen. Shows a start button; on click, hides the title UI,
///     disables the title camera, and calls ComicManager.StartComic().
///
///     Camera transition from title to Panel 1 is controlled by Panel 1's IncomingBlend
///     on its PanelDataSO — set to EaseInOut for a cinematic tilt-down, Cut for instant.
///
///     Scene setup:
///     1. Add a CinemachineCamera for the title view; leave it ENABLED in the scene so
///        Cinemachine uses it as the blend origin when StartComic() fires.
///     2. Add a UI Canvas with title art and a Button; assign them below.
///     3. Assign the ComicManager from the scene.
/// </summary>
public class TitleScreenController : MonoBehaviour
{
    #region Variables

    [Tooltip("The ComicManager in the scene. Called to begin panel sequence on start.")]
    [SerializeField] private ComicManager comicManager;

    [Tooltip("The CinemachineCamera framing the title screen. Disabled when the comic starts.")]
    [SerializeField] private CinemachineCamera titleCamera;

    [Tooltip("The root GameObject of the title screen UI (Canvas or panel). Hidden when the comic starts.")]
    [SerializeField] private GameObject titleUI;

    [Tooltip("The start button the player presses to begin.")]
    [SerializeField] private Button startButton;

    #endregion

    #region Methods

    private void Start()
    {
        if (comicManager == null)
            Debug.LogError("[TitleScreenController] ComicManager is not assigned.", this);
        if (titleCamera == null)
            Debug.LogError("[TitleScreenController] Title Camera is not assigned.", this);
        if (titleUI == null)
            Debug.LogError("[TitleScreenController] Title UI is not assigned.", this);
        if (startButton == null)
            Debug.LogError("[TitleScreenController] Start Button is not assigned.", this);

        startButton?.onClick.AddListener(OnStartClicked);
    }

    private void OnDestroy()
    {
        startButton?.onClick.RemoveListener(OnStartClicked);
    }

    private void OnStartClicked()
    {
        // Hide title UI immediately so it doesn't overlay the comic.
        if (titleUI != null)
            titleUI.SetActive(false);

        // Start the comic — SwitchCameraCommand will enable Panel 1's camera and set the
        // brain's blend to Panel 1's IncomingBlend before the title camera is disabled,
        // so Cinemachine blends from the title camera's position to Panel 1's position.
        comicManager?.StartComic();

        // Disable the title camera after StartComic() so Cinemachine has a blend origin.
        // The brain retains the last-known position of the title camera for the blend duration.
        if (titleCamera != null)
            titleCamera.enabled = false;
    }

    #endregion
}