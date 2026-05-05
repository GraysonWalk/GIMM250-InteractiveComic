using UnityEngine;

/// <summary>
///     Holds static configuration for a single music cue: which clip to play, how loud, whether to loop,
///     and how long to fade in / out by default. Designer-editable assets live under Assets/Data/Music/.
///
///     A MusicTrackSO with <see cref="Clip" /> = null represents intentional silence — assign one to a
///     panel (or pass to <see cref="MusicController.Play" />) to fade the current music out without
///     starting a new track. Leaving <see cref="PanelDataSO.Music" /> empty (null SO) means "no change".
/// </summary>
[CreateAssetMenu(fileName = "MusicTrackSO", menuName = "Comic/Music Track", order = 0)]
public class MusicTrackSO : ScriptableObject
{
    #region Variables

    [Tooltip("The audio clip to play. Leave null to represent silence — assign this SO to a panel " +
             "to fade the current music out without starting a new track.")]
    [SerializeField] private AudioClip clip;

    [Tooltip("Target volume in decibels when this track is fully faded in. 0 dB is unattenuated, " +
             "-80 dB is silent. Use this to balance loud and quiet tracks against each other; " +
             "do not bake mix balance into the source files.")]
    [SerializeField, Range(-80f, 0f)] private float targetVolumeDb;

    [Tooltip("Whether the clip loops while playing. Underscore music should loop; one-shot stingers should not.")]
    [SerializeField] private bool loop = true;

    [Tooltip("Default fade-in duration in seconds when transitioning TO this track. " +
             "MusicController.Play() uses this unless an override is supplied.")]
    [SerializeField, Min(0f)] private float defaultFadeInSeconds = 1f;

    [Tooltip("Default fade-out duration in seconds when transitioning AWAY from this track. " +
             "MusicController.Play() reads this from the OUTGOING track when crossfading.")]
    [SerializeField, Min(0f)] private float defaultFadeOutSeconds = 1f;

    public AudioClip Clip => clip;
    public float TargetVolumeDb => targetVolumeDb;
    public bool Loop => loop;
    public float DefaultFadeInSeconds => defaultFadeInSeconds;
    public float DefaultFadeOutSeconds => defaultFadeOutSeconds;

    #endregion
}
