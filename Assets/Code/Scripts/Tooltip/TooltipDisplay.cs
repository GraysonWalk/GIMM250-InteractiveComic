using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
///     Displays tooltips raised by any <see cref="TooltipTrigger" /> in the scene.
///     Place this component anywhere inside your Canvas hierarchy.
///     The tooltipPanel child must have its anchor set to centre (0.5, 0.5) and be a
///     direct child of the Canvas root so its local space matches screen space.
/// </summary>
public class TooltipDisplay : MonoBehaviour
{
    #region Variables

    [Tooltip("The child GameObject with the background and label. This is what gets shown/hidden.")]
    [SerializeField] private GameObject tooltipPanel;

    [SerializeField] private TMP_Text label;

    [Tooltip("Distance in pixels to nudge the tooltip away from the cursor tip. " +
             "Both components are treated as magnitudes — sign is determined automatically " +
             "based on which side of the cursor the tooltip is on.")]
    [SerializeField] private Vector2 cursorOffset = new(12f, 12f);

    [Tooltip("Seconds for the tooltip to fully fade in or out.")]
    [SerializeField] private float fadeDuration = 0.15f;

    private RectTransform _tooltipRect;
    private CanvasGroup _canvasGroup;
    private Canvas _canvas;
    private Coroutine _fadeCoroutine;

    #endregion

    #region Methods

    private void OnValidate()
    {
        if (tooltipPanel != null && tooltipPanel.GetComponent<Canvas>() != null)
            Debug.LogError("[TooltipDisplay] tooltipPanel is assigned a Canvas — " +
                           "this slot should be the small visual child panel (background + label), not the Canvas itself.", this);

        if (tooltipPanel != null && tooltipPanel.activeSelf)
            Debug.LogWarning("[TooltipDisplay] tooltipPanel is saved as active — " +
                             "set it inactive in the scene so it doesn't flash on startup.", this);
    }

    private void Awake()
    {
        _canvas = GetComponentInParent<Canvas>();
        if (_canvas == null)
            Debug.LogError("[TooltipDisplay] No Canvas found in parent hierarchy.", this);

        if (tooltipPanel != null)
        {
            _tooltipRect = tooltipPanel.GetComponent<RectTransform>();
            _canvasGroup = tooltipPanel.GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
            {
                Debug.LogWarning("[TooltipDisplay] tooltipPanel has no CanvasGroup — add one in the Editor.", this);
                return;
            }

            // Prevent the tooltip from stealing pointer events from the hovered element,
            // which would cause an enter/exit flicker loop.
            _canvasGroup.blocksRaycasts = false;
            _canvasGroup.alpha          = 0f;

            tooltipPanel.SetActive(false);
        }
    }

    private void OnEnable()
    {
        TooltipTrigger.OnShow += Show;
        TooltipTrigger.OnHide += Hide;
    }

    private void OnDisable()
    {
        TooltipTrigger.OnShow -= Show;
        TooltipTrigger.OnHide -= Hide;
    }

    private void Update()
    {
        if (_tooltipRect == null || Mouse.current == null || _canvas == null || !tooltipPanel.activeSelf) return;

        Vector2 rawPos = Mouse.current.position.ReadValue();

        // Flip the tooltip to the opposite side of the cursor when near a screen edge.
        float pivotX = rawPos.x / Screen.width  > 0.5f ? 1f : 0f;
        float pivotY = rawPos.y / Screen.height > 0.5f ? 1f : 0f;
        _tooltipRect.pivot = new Vector2(pivotX, pivotY);

        // Nudge the pivot away from the cursor tip; sign flips with pivot direction.
        Vector2 signedOffset = new Vector2(
            pivotX < 0.5f ?  Mathf.Abs(cursorOffset.x) : -Mathf.Abs(cursorOffset.x),
            pivotY < 0.5f ?  Mathf.Abs(cursorOffset.y) : -Mathf.Abs(cursorOffset.y));

        Camera cam = _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _canvas.worldCamera;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            (RectTransform)_tooltipRect.parent, rawPos + signedOffset, cam, out Vector2 localPoint);

        _tooltipRect.anchoredPosition = localPoint;
    }

    private void Show(string message)
    {
        if (label != null) label.text = message;
        if (tooltipPanel == null) return;

        tooltipPanel.SetActive(true);
        SetFade(1f, deactivateOnComplete: false);
    }

    private void Hide()
    {
        if (tooltipPanel == null || !tooltipPanel.activeSelf) return;

        SetFade(0f, deactivateOnComplete: true);
    }

    private void SetFade(float targetAlpha, bool deactivateOnComplete)
    {
        if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
        _fadeCoroutine = StartCoroutine(Fade(targetAlpha, deactivateOnComplete));
    }

    private IEnumerator Fade(float targetAlpha, bool deactivateOnComplete)
    {
        float startAlpha = _canvasGroup.alpha;
        // Scale duration by the actual alpha distance so interrupted fades don't feel slow.
        float duration = fadeDuration * Mathf.Abs(targetAlpha - startAlpha);
        float elapsed  = 0f;

        while (elapsed < duration)
        {
            elapsed          += Time.unscaledDeltaTime;
            _canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / duration);
            yield return null;
        }

        _canvasGroup.alpha = targetAlpha;
        if (deactivateOnComplete) tooltipPanel.SetActive(false);
    }

    #endregion
}
