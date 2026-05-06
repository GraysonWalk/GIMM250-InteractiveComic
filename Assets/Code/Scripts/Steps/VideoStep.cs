using System.Collections;
using UnityEngine;
using UnityEngine.Video;

/// <summary>
///     A blocking step that plays a VideoPlayer clip. Blocks Advance() until the video reaches
///     its end (loopPointReached fires), then unblocks so the player can continue.
///
///     <b>Render Mode requirement:</b> the VideoPlayer's Render Mode must be set to
///     <b>Render Texture</b>. Material Override clears the material texture when the
///     GameObject is deactivated, causing grey on every revisit. A RenderTexture asset
///     persists independently of the VideoPlayer lifecycle — the last frame is preserved
///     across panel hides and loop revisits automatically.
///
///     <b>Loops In Background mode:</b> tick <i>Loops In Background</i> and enable Loop on the
///     VideoPlayer to have the video play continuously while the player advances through subsequent
///     steps. The step fires OnStepComplete on the next frame after activation so ComicPanel
///     unblocks immediately — the video keeps looping until the panel is hidden.
///
///     <b>One-shot mode (default):</b> leave <i>Loops In Background</i> unticked and disable Loop
///     on the VideoPlayer. The step blocks until loopPointReached fires, then pauses on the last
///     frame so frozen revisits show the end frame automatically.
///
///     Designer workflow:
///     1. Add a child GameObject under ComicPanel; attach this component + a VideoPlayer.
///     2. Create a RenderTexture asset (Project → Create → Render Texture). Set its resolution
///        to match your video. Assign it to the VideoPlayer's Target Texture field.
///     3. Apply the same RenderTexture to the material shown in the panel (e.g. mesh material's
///        Base Map, or a UI RawImage's Texture field).
///     4. One-shot: disable Loop. Tick Persists In Final State to freeze on last frame in history.
///        Looping: enable Loop and tick Loops In Background.
///     5. Tick Replay On Revisit to replay the video on subsequent loops (default true).
/// </summary>
[RequireComponent(typeof(VideoPlayer))]
public sealed class VideoStep : StepBase
{
    #region Variables

    private const float SafetyGraceSeconds = 1f;

    [Tooltip("If ticked, the video loops continuously in the background while the player advances " +
             "through subsequent steps. The step unblocks immediately on activation. " +
             "Requires Loop to be enabled on the VideoPlayer component.")]
    [SerializeField] private bool loopsInBackground;

    [Tooltip("If ticked, this element remains visible when the panel reaches its final state. " +
             "One-shot: shows the last frame (preserved in the RenderTexture). " +
             "Only meaningful when Loops In Background is unticked for one-shot clips.")]
    [SerializeField] private bool persistsInFinalState;

    public override bool ShowInFinalState => persistsInFinalState && HasBeenActivated;

    private VideoPlayer _video;
    private bool _completedThisActivation;
    private Coroutine _safetyCoroutine;

    #endregion

    #region Methods

    private void Awake()
    {
        _video = GetComponent<VideoPlayer>();

        if (_video.renderMode == VideoRenderMode.MaterialOverride)
            Debug.LogError(
                $"[VideoStep] '{gameObject.name}' is using Material Override render mode. " +
                "Switch to Render Texture — Material Override clears the texture when the GameObject " +
                "is deactivated, causing grey on every revisit.", this);

        if (loopsInBackground && !_video.isLooping)
            Debug.LogWarning(
                $"[VideoStep] '{gameObject.name}' has Loops In Background ticked but Loop is " +
                "disabled on the VideoPlayer. Enable Loop on the VideoPlayer.", this);

        if (!loopsInBackground && _video.isLooping)
            Debug.LogWarning(
                $"[VideoStep] '{gameObject.name}' has Loop enabled but Loops In Background is " +
                "unticked. loopPointReached will never fire — the step will softlock. " +
                "Either enable Loops In Background or disable Loop on the VideoPlayer.", this);
    }

    /// <summary>
    ///     One-shot: plays the video and blocks until loopPointReached fires.
    ///     Looping: starts the loop and unblocks on the next frame.
    ///     Revisit frozen (hideOnRevisit = false): RenderTexture already holds the last frame from
    ///     Pause() — just re-enable the GameObject. Looping restarts the video.
    ///     Revisit hidden (hideOnRevisit = true): stays deactivated; auto-chained.
    /// </summary>
    public override void Activate(PlayerChoicesSO choices)
    {
        bool skip = BeginActivation();
        _completedThisActivation = false;
        StopSafetyCoroutine();

        if (!skip)
        {
            gameObject.SetActive(true);
            // Prepare before playing: Prepare() decodes the first frame into the RenderTexture so
            // playback starts without a grey flash. Do NOT call Stop() first — Stop() clears the
            // RenderTexture, causing a grey frame while the decode thread catches up.
            _safetyCoroutine = StartCoroutine(PrepareAndPlay());
        }
        else if (!hideOnRevisit)
        {
            gameObject.SetActive(true);
            if (loopsInBackground)
            {
                // Restart the loop.
                _safetyCoroutine = StartCoroutine(PrepareAndPlay());
            }
            // One-shot frozen: Deactivate() called Pause() which preserved the last frame in the
            // RenderTexture. Re-enabling the GameObject is sufficient — no seek or repaint needed.
        }
        // else: hideOnRevisit — stay deactivated; auto-chained.
    }

    /// <summary>
    ///     Pauses the video to preserve the last frame in the RenderTexture, then hides the GameObject.
    ///     The RenderTexture retains its content after deactivation — no grey on revisit.
    /// </summary>
    public override void Deactivate()
    {
        UnsubscribeLoopEvent();
        StopSafetyCoroutine();
        _video.Pause();     // Pause (not Stop) — preserves last frame in RenderTexture.
        base.Deactivate();
    }

    /// <summary>Stops the video so the next Activate() replays from frame 0.</summary>
    public override void PrepareForReplay()
    {
        base.PrepareForReplay();
        UnsubscribeLoopEvent();
        _video.Stop();
    }

    private void OnVideoComplete(VideoPlayer vp)
    {
        if (_completedThisActivation) return;
        _completedThisActivation = true;
        UnsubscribeLoopEvent();
        StopSafetyCoroutine();
        _video.Pause();
        OnStepComplete.Invoke();
    }

    // Prepares the VideoPlayer (loads first frame into RenderTexture without playing),
    // then starts playback. Does NOT call Stop() first — that would clear the RenderTexture.
    // After preparation, behaviour splits:
    //   loopsInBackground = true  → fire OnStepComplete next frame (unblock immediately)
    //   loopsInBackground = false → subscribe loopPointReached and start the safety fallback
    private IEnumerator PrepareAndPlay()
    {
        _video.Prepare();
        while (!_video.isPrepared) yield return null;
        _video.Play();

        if (loopsInBackground)
        {
            // Unblock on the next frame so ComicPanel has time to add its UnblockPanel
            // listener after Activate() returns before OnStepComplete fires.
            yield return null;
            _completedThisActivation = true;
            OnStepComplete.Invoke();
        }
        else
        {
            _video.loopPointReached += OnVideoComplete;
            _safetyCoroutine = StartCoroutine(SafetyFallback());
        }
    }

    // One-shot safety net: fires OnStepComplete if loopPointReached never arrives,
    // preventing softlocks if the clip fails to load or buffer.
    // Only started after Prepare() completes, so _video.length is always valid.
    private IEnumerator SafetyFallback()
    {
        float deadline = Time.time + (float)_video.length + SafetyGraceSeconds;

        while (Time.time < deadline)
        {
            if (_completedThisActivation) yield break;
            yield return null;
        }

        if (_completedThisActivation) yield break;

        Debug.LogWarning(
            $"[VideoStep] '{gameObject.name}' clip finished but loopPointReached never fired " +
            $"within {(float)_video.length + SafetyGraceSeconds:0.00}s. Auto-completing the step. " +
            "Ensure Loop is disabled on the VideoPlayer to silence this warning.", this);

        _completedThisActivation = true;
        _video.Pause();
        OnStepComplete.Invoke();
    }

    private void StopSafetyCoroutine()
    {
        if (_safetyCoroutine == null) return;
        StopCoroutine(_safetyCoroutine);
        _safetyCoroutine = null;
    }

    private void UnsubscribeLoopEvent()
    {
        _video.loopPointReached -= OnVideoComplete;
    }

    private void Reset()
    {
        gameObject.SetActive(false);
        replayOnRevisit = true;
    }

    #endregion
}
