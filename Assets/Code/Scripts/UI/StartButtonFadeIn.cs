using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
///     Fades the button wrapper in on enable via a CanvasGroup.
///
///     Attach this script and its required CanvasGroup to a parent wrapper object.
///     The Button and its Animator should be children of the wrapper so the Button Animator
///     cannot target this CanvasGroup.
///
///     The Button's text must be initialised with alpha=0 in the prefab so it does not
///     flash visible before the CanvasGroup fade begins. The Button's Animated transition is
///     temporarily set to None during the fade to prevent its clips writing alpha values that
///     fight the coroutine; it is restored once the wrapper is fully visible.
///
///     Scene hierarchy:
///         StartButtonWrapper        ← this script + CanvasGroup live here (CanvasGroup alpha = 0)
///           └── StartButton         ← Button + Animator live here; Text alpha saved as 0
///                 └── Text
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class StartButtonFadeIn : MonoBehaviour
{
    #region Variables

    [SerializeField] private float duration = 0.6f;
    [SerializeField] private float delay;

    private CanvasGroup _cg;
    private Button _button;
    private Animator _animator;

    #endregion

    #region Methods

    // Called by Unity when the component is first added or Reset in the Inspector.
    // Saves the wrapper with alpha=0 so it is invisible before the fade starts.
    private void Reset()
    {
        var cg = GetComponent<CanvasGroup>();
        if (cg != null) cg.alpha = 0f;
    }

    private void Awake()
    {
        _cg = GetComponent<CanvasGroup>();
        _button = GetComponentInChildren<Button>();
        _animator = GetComponentInChildren<Animator>();

        _cg.alpha = 0f;
        _cg.interactable = true;
        _cg.blocksRaycasts = false;

        // Silence the Button's animated transition during the fade so its clips
        // cannot write alpha/color values that fight the CanvasGroup coroutine.
        if (_button != null) _button.transition = Selectable.Transition.None;
        if (_animator != null) _animator.enabled = false;
    }

    private void OnEnable()
    {
        StartCoroutine(FadeIn());
    }

    private IEnumerator FadeIn()
    {
        // Skip the first frame to avoid the large delta-time spike that Unity
        // accumulates during scene initialisation in Play Mode.
        yield return null;

        if (delay > 0f) yield return new WaitForSeconds(delay);
        var t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            _cg.alpha = Mathf.Clamp01(t / duration);
            yield return null;
        }

        _cg.alpha = 1f;

        // Restore the animated transition now that the button is fully visible.
        if (_animator != null) _animator.enabled = true;
        if (_button != null) _button.transition = Selectable.Transition.Animation;
        _cg.blocksRaycasts = true;
    }

    #endregion
}