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
    [Tooltip("CanvasGroup on the Back button — controls visibility without restarting its Animator.")]
    [SerializeField] private CanvasGroup backGroup;

    [Tooltip("CanvasGroup on the Forward button — controls visibility without restarting its Animator.")]
    [SerializeField] private CanvasGroup forwardGroup;

    [Header("Replay")]
    [Tooltip("Button shown when the player is browsing history. Replays the current panel's animation.")]
    [SerializeField] private Button replayButton;

    [Header("Advance Hint")]
    [Tooltip("Text shown after the panel intro animation finishes and spacebar advance is available.")]
    [SerializeField] private TMP_Text advanceHintText;

    // True when the player is at the story front (no redo history).
    // The hint is only shown once the intro animation also finishes.
    private bool _advanceStructurallyAvailable;
    private CanvasGroup _replayGroup;

    #endregion

    #region Methods

    private void OnValidate()
    {
        if (comicManager == null)
            Debug.LogError("[NavigationPresenter] comicManager is not assigned.", this);
    }

    private void Awake()
    {
        if (replayButton != null)
        {
            _replayGroup = replayButton.GetComponent<CanvasGroup>();
            if (_replayGroup == null)
                Debug.LogWarning("[NavigationPresenter] replayButton has no CanvasGroup — add one in the Editor.", replayButton);
        }

        SetVisible(backGroup,    false);
        SetVisible(forwardGroup, false);
        SetVisible(_replayGroup, false);
        if (advanceHintText != null) advanceHintText.gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        if (comicManager == null)
        {
            Debug.LogError("[NavigationPresenter] comicManager is not assigned — presenter will not function.", this);
            return;
        }

        comicManager.OnReplayAvailabilityChanged.AddListener(OnReplayAvailabilityChanged);
        comicManager.OnNavigationAvailabilityChanged.AddListener(OnNavigationAvailabilityChanged);
        comicManager.OnCurrentPanelReadyForInput.AddListener(OnPanelReadyForInput);
        if (replayButton != null)
            replayButton.onClick.AddListener(comicManager.ReplayCurrentPanel);
    }

    private void OnDisable()
    {
        if (comicManager == null) return;

        comicManager.OnReplayAvailabilityChanged.RemoveListener(OnReplayAvailabilityChanged);
        comicManager.OnNavigationAvailabilityChanged.RemoveListener(OnNavigationAvailabilityChanged);
        comicManager.OnCurrentPanelReadyForInput.RemoveListener(OnPanelReadyForInput);
        if (replayButton != null)
            replayButton.onClick.RemoveListener(comicManager.ReplayCurrentPanel);
    }

    private void OnReplayAvailabilityChanged(bool replayAvailable)
    {
        SetVisible(_replayGroup, replayAvailable);
    }

    private void OnNavigationAvailabilityChanged(bool canGoBack, bool canGoForward)
    {
        SetVisible(backGroup,    canGoBack);
        SetVisible(forwardGroup, canGoForward);

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

    /// <summary>Shows or hides a button via CanvasGroup without touching SetActive.</summary>
    private static void SetVisible(CanvasGroup group, bool visible)
    {
        if (group == null) return;
        group.alpha          = visible ? 1f : 0f;
        group.blocksRaycasts = visible;
    }

    #endregion
}