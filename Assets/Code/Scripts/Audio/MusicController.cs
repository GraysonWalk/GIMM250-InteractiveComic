using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

/// <summary>
///     Plays and crossfades the comic's music tracks via two AudioSources routed through two
///     AudioMixer groups (MusicA, MusicB). Fades are performed in dB space by setting the mixer's
///     exposed volume parameters — never by lerping AudioSource.volume — so that the project mix
///     bus remains the single source of truth for volume.
///
///     Subscribes to <see cref="ComicManager.OnDisplayedPanelChanged" />: whenever the displayed
///     panel changes (forward, history, replay), the controller crossfades to that panel's
///     <see cref="MusicTrackSO" />. A null <c>Music</c> field on the panel means "no change".
///     A non-null SO with a null Clip means "fade to silence".
///
///     Mixer setup (one-time):
///     1. In MixerMain.mixer, add two child groups under Music: "MusicA" and "MusicB".
///     2. Right-click each group's Volume slider → "Expose 'Volume (of MusicA)' to script".
///        Rename the exposed parameters in the Audio Mixer's Exposed Parameters list to
///        match <see cref="volumeParamA" /> and <see cref="volumeParamB" /> below.
///     3. Drag MusicA / MusicB into the AudioMixerGroup slots and assign the AudioSources to
///        their corresponding Output fields in the Inspector.
/// </summary>
public class MusicController : MonoBehaviour
{
    #region Variables

    [Header("Mixer")]
    [Tooltip("The AudioMixer holding the MusicA and MusicB groups whose volume parameters are " +
             "driven by the crossfade. Drag MixerMain.mixer here.")]
    [SerializeField] private AudioMixer mixer;

    [Tooltip("Name of the exposed Volume parameter on the MusicA group (in dB). " +
             "Must match the Exposed Parameters list in the AudioMixer asset.")]
    [SerializeField] private string volumeParamA = "MusicVolumeA";

    [Tooltip("Name of the exposed Volume parameter on the MusicB group (in dB). " +
             "Must match the Exposed Parameters list in the AudioMixer asset.")]
    [SerializeField] private string volumeParamB = "MusicVolumeB";

    [Header("Sources")]
    [Tooltip("AudioSource routed through the MusicA mixer group.")]
    [SerializeField] private AudioSource sourceA;

    [Tooltip("AudioSource routed through the MusicB mixer group.")]
    [SerializeField] private AudioSource sourceB;

    [Header("Defaults")]
    [Tooltip("dB value treated as silent. -80 dB is the AudioMixer minimum.")]
    [SerializeField, Range(-80f, 0f)] private float silenceDb = -80f;

    [Tooltip("Fallback fade duration in seconds, used when a fade is requested but neither the " +
             "outgoing nor incoming track supplies one (e.g. cold start with no current track).")]
    [SerializeField, Min(0f)] private float fallbackFadeSeconds = 1f;

    [Header("Subscriptions")]
    [Tooltip("ComicManager whose OnDisplayedPanelChanged event drives per-panel music swaps. " +
             "Optional — if null, only manual Play()/Stop() calls take effect.")]
    [SerializeField] private ComicManager comicManager;

    private AudioSource _activeSource;
    private AudioSource _idleSource;
    private string _activeParam;
    private string _idleParam;
    private MusicTrackSO _currentTrack;
    private Coroutine _fadeRoutine;

    #endregion

    #region Methods

    private void Awake()
    {
        // Source A starts as the active (current) source; B is the idle (next) source.
        // Both groups are silenced explicitly at Awake so the scene starts mute regardless of
        // any default values left in the mixer asset.
        _activeSource = sourceA;
        _idleSource = sourceB;
        _activeParam = volumeParamA;
        _idleParam = volumeParamB;

        if (mixer != null)
        {
            mixer.SetFloat(volumeParamA, silenceDb);
            mixer.SetFloat(volumeParamB, silenceDb);
        }
    }

    private void OnEnable()
    {
        if (comicManager != null)
            comicManager.OnDisplayedPanelChanged.AddListener(OnDisplayedPanelChanged);
    }

    private void OnDisable()
    {
        if (comicManager != null)
            comicManager.OnDisplayedPanelChanged.RemoveListener(OnDisplayedPanelChanged);
    }

    private void OnValidate()
    {
        if (mixer == null)
            Debug.LogWarning("[MusicController] AudioMixer is not assigned. " +
                             "Drag MixerMain.mixer into the slot.", this);
        if (sourceA == null || sourceB == null)
            Debug.LogWarning("[MusicController] sourceA or sourceB is not assigned. " +
                             "Add two AudioSource children and assign them.", this);
    }

    /// <summary>
    ///     Crossfades from the currently-playing track to <paramref name="track" />. If the same
    ///     track is already playing, the call is a no-op. If the track's <see cref="MusicTrackSO.Clip" />
    ///     is null, fades the current track to silence without starting a new one.
    /// </summary>
    public void Play(MusicTrackSO track)
    {
        if (ReferenceEquals(track, _currentTrack)) return;

        float fadeOut = _currentTrack != null ? _currentTrack.DefaultFadeOutSeconds : fallbackFadeSeconds;
        float fadeIn = track != null ? track.DefaultFadeInSeconds : fallbackFadeSeconds;

        _currentTrack = track;
        StartFade(track, fadeOut, fadeIn);
    }

    /// <summary>Fades the current track to silence using its own <c>DefaultFadeOutSeconds</c>.</summary>
    public void Stop() => Play(null);

    private void OnDisplayedPanelChanged(IComicPanel panel)
    {
        // A null Music field on the panel means "do not change the music" — most panels.
        // To explicitly stop music on a panel, assign a MusicTrackSO with Clip = null.
        if (panel?.Music == null) return;
        Play(panel.Music);
    }

    private void StartFade(MusicTrackSO incoming, float fadeOut, float fadeIn)
    {
        if (_fadeRoutine != null)
            StopCoroutine(_fadeRoutine);
        _fadeRoutine = StartCoroutine(FadeRoutine(incoming, fadeOut, fadeIn));
    }

    /// <summary>
    ///     Ramps the active source's mixer volume to silence while ramping the idle source's volume
    ///     to the incoming track's TargetVolumeDb. Each side respects its own duration; the loop
    ///     runs for the longer of the two, after which the sources swap roles.
    /// </summary>
    private IEnumerator FadeRoutine(MusicTrackSO incoming, float fadeOut, float fadeIn)
    {
        AudioSource fadingOut = _activeSource;
        string fadingOutParam = _activeParam;
        AudioSource fadingIn = _idleSource;
        string fadingInParam = _idleParam;

        float startOutDb = GetDb(fadingOutParam);
        float startInDb = GetDb(fadingInParam);
        float targetInDb = incoming != null && incoming.Clip != null ? incoming.TargetVolumeDb : silenceDb;

        // Configure the incoming clip on the idle source and start it playing silent.
        // The mixer ramp will bring it up over fadeIn seconds.
        if (incoming != null && incoming.Clip != null)
        {
            fadingIn.clip = incoming.Clip;
            fadingIn.loop = incoming.Loop;
            fadingIn.Play();
        }

        float duration = Mathf.Max(fadeOut, fadeIn);
        if (duration <= 0f)
        {
            SetDb(fadingOutParam, silenceDb);
            SetDb(fadingInParam, targetInDb);
            FinishFade(fadingOut, fadingIn, fadingOutParam, fadingInParam);
            yield break;
        }

        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;

            float outProgress = fadeOut > 0f ? Mathf.Clamp01(t / fadeOut) : 1f;
            float inProgress = fadeIn > 0f ? Mathf.Clamp01(t / fadeIn) : 1f;

            SetDb(fadingOutParam, Mathf.Lerp(startOutDb, silenceDb, outProgress));
            SetDb(fadingInParam, Mathf.Lerp(startInDb, targetInDb, inProgress));

            yield return null;
        }

        SetDb(fadingOutParam, silenceDb);
        SetDb(fadingInParam, targetInDb);
        FinishFade(fadingOut, fadingIn, fadingOutParam, fadingInParam);
    }

    /// <summary>
    ///     Stops the source that just faded out, swaps active / idle roles, and clears the fade routine.
    ///     The source that faded in becomes the active source for the next transition.
    /// </summary>
    private void FinishFade(AudioSource fadedOut, AudioSource fadedIn, string fadedOutParam, string fadedInParam)
    {
        fadedOut.Stop();
        fadedOut.clip = null;

        _activeSource = fadedIn;
        _idleSource = fadedOut;
        _activeParam = fadedInParam;
        _idleParam = fadedOutParam;
        _fadeRoutine = null;
    }

    private float GetDb(string paramName)
    {
        return mixer != null && mixer.GetFloat(paramName, out float db) ? db : silenceDb;
    }

    private void SetDb(string paramName, float db)
    {
        if (mixer != null) mixer.SetFloat(paramName, db);
    }

    #endregion
}
