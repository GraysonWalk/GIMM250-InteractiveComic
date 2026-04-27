using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
///     Handles the presentation of the navigation UI: back/forward arrows and the Replay button.
/// </summary>
public class NavigationPresenter : MonoBehaviour
{
    #region Variables

    [SerializeField] private ComicManager comicManager;

    [Header("Navigation Arrows")]
    [SerializeField] private Button backButton;

    [SerializeField] private Button forwardButton;

    [Header("Replay")]
    [Tooltip("Button shown when the player is browsing history. Replays the current panel's animation.")]
    [SerializeField] private Button replayButton;

    [Header("Advance Hint")]
    [Tooltip("Text shown after the panel intro animation finishes and spacebar advance is available.")]
    [SerializeField] private TMP_Text advanceHintText;

    // True when the player is at the story front (no redo history).
    // The hint is only shown once the intro animation also finishes.
    private bool _advanceStructurallyAvailable;

    #endregion

    #region Methods

    private void Awake()
    {
        // Hide everything before any Start() fires so there's no single-frame flash.
        if (backButton != null) backButton.interactable = false;
        if (forwardButton != null) forwardButton.interactable = false;
        if (replayButton != null) replayButton.gameObject.SetActive(false);
        if (advanceHintText != null) advanceHintText.gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        comicManager.OnReplayAvailabilityChanged.AddListener(OnReplayAvailabilityChanged);
        comicManager.OnNavigationAvailabilityChanged.AddListener(OnNavigationAvailabilityChanged);
        comicManager.OnCurrentPanelReadyForInput.AddListener(OnPanelReadyForInput);
        if (replayButton != null)
            replayButton.onClick.AddListener(comicManager.ReplayCurrentPanel);
    }

    private void OnDisable()
    {
        comicManager.OnReplayAvailabilityChanged.RemoveListener(OnReplayAvailabilityChanged);
        comicManager.OnNavigationAvailabilityChanged.RemoveListener(OnNavigationAvailabilityChanged);
        comicManager.OnCurrentPanelReadyForInput.RemoveListener(OnPanelReadyForInput);
        if (replayButton != null)
            replayButton.onClick.RemoveListener(comicManager.ReplayCurrentPanel);
    }

    private void OnReplayAvailabilityChanged(bool replayAvailable)
    {
        if (replayButton != null)
            replayButton.gameObject.SetActive(replayAvailable);
    }

    private void OnNavigationAvailabilityChanged(bool canGoBack, bool canGoForward)
    {
        if (backButton != null) backButton.interactable = canGoBack;
        if (forwardButton != null) forwardButton.interactable = canGoForward;
        // Advance is available only at the story front (no forward history).
        _advanceStructurallyAvailable = !canGoForward;
        // Always hide on any navigation change — it re-appears once the new panel fires
        // OnReadyForInput (after its intro animation finishes), preventing the hint from
        // persisting through panel transitions or while the player is in history.
        if (advanceHintText != null) advanceHintText.gameObject.SetActive(false);
    }

    /// <summary>
    ///     Called whenever the panel is ready for the next Advance Input button press.
    ///     Shows the advance hint only if the player is also at the story front.
    /// </summary>
    private void OnPanelReadyForInput()
    {
        if (_advanceStructurallyAvailable && advanceHintText != null)
            advanceHintText.gameObject.SetActive(true);
    }

    #endregion
}