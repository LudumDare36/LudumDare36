using UnityEngine;
using System.Linq;
using System.Collections;

public class SfxHandler : MonoBehaviour
{
    [Header("SFX Priority")]
    [Space(1)]
    [Range(0, 256)]
    public int sfxMasterPriority = 180;
    [Range(0, 1)]
    public float sfxAirWooshPriority = 0.999f;
    [Range(0, 1)]
    public float sfxMetalWooshPriority = 0.666f;
    [Range(0, 1)]
    public float sfxGruntPriority = 0.666f;
    [Range(0, 1)]
    public float sfxShoutPriority = 0.666f;
    [Space(2)]


    [Header("SFX Volume")]
    [Space(1)]
    [Range(0, 1)]
    public float sfxMasterVolume = 1f;
    [Range(0, 1)]
    public float sfxAirWooshVolume = 0.999f;
    [Range(0, 1)]
    public float sfxMetalWooshVolume = 0.666f;
    [Range(0, 1)]
    public float sfxGruntVolume = 0.666f;
    [Range(0, 1)]
    public float sfxShoutVolume = 0.666f;
    [Space(2)]


    [Header("SFX Behavior")]
    [Space(1)]
    [Range(0, 1)]
    public float sfxSpatialBlend = 0.666f;
    [Range(0, 360)]
    public int sfxSpread = 90;
    [Range(0, 90)]
    public int sfxMaxDistance = 90;
    [Space]

    [Header("SFX Sources")]
    [Space(1)]
    [SerializeField]
    private AudioSource[] airWooshVariants;
    [SerializeField]
    private AudioSource[] metalWooshVariants;
    [SerializeField]
    private AudioSource[] gruntsVariants;
    [SerializeField]
    private AudioSource[] shoutVariants;


    private void Start() { StartCoroutine(SetUpSFX()); }

    IEnumerator SetUpSFX()
    {
        yield return new WaitForFixedUpdate();
        airWooshVariants = GetComponents<AudioSource>().
            Where(x => x.clip.name.Contains("AirWoosh")).ToArray();

        metalWooshVariants = GetComponents<AudioSource>().
            Where(x => x.clip.name.Contains("MetalWoosh")).ToArray();

        gruntsVariants = GetComponents<AudioSource>().
            Where(x => x.clip.name.Contains("Grunt")).ToArray();

        shoutVariants = GetComponents<AudioSource>().
            Where(x => x.clip.name.Contains("Shout")).ToArray();


        foreach (var _sndSource in airWooshVariants)
        {
            _sndSource.playOnAwake = false;
            _sndSource.loop = false;
            _sndSource.priority = Mathf.RoundToInt(sfxAirWooshPriority * sfxMasterPriority);
            _sndSource.volume = sfxAirWooshVolume * sfxMasterVolume;
            _sndSource.spatialBlend = sfxSpatialBlend;
            _sndSource.spread = sfxSpread;
        }

        foreach (var _sndSource in metalWooshVariants)
        {
            _sndSource.playOnAwake = false;
            _sndSource.loop = false;
            _sndSource.priority = Mathf.RoundToInt(sfxMetalWooshPriority * sfxMasterPriority);
            _sndSource.volume = sfxMetalWooshVolume * sfxMasterVolume;
            _sndSource.spatialBlend = sfxSpatialBlend;
            _sndSource.spread = sfxSpread;
            _sndSource.maxDistance = sfxMaxDistance;
        }

        foreach (var _sndSource in gruntsVariants)
        {
            _sndSource.playOnAwake = false;
            _sndSource.loop = false;
            _sndSource.priority = Mathf.RoundToInt(sfxGruntPriority * sfxMasterPriority);
            _sndSource.volume = sfxGruntVolume * sfxMasterVolume;
            _sndSource.spatialBlend = sfxSpatialBlend;
            _sndSource.spread = sfxSpread;
            _sndSource.maxDistance = sfxMaxDistance;
        }

        foreach (var _sndSource in shoutVariants)
        {
            _sndSource.playOnAwake = false;
            _sndSource.loop = false;
            _sndSource.priority = Mathf.RoundToInt(sfxShoutPriority * sfxMasterPriority);
            _sndSource.volume = sfxShoutVolume * sfxMasterVolume;
            _sndSource.spatialBlend = sfxSpatialBlend;
            _sndSource.spread = sfxSpread;
            _sndSource.maxDistance = sfxMaxDistance;
        }
    }

    private IEnumerator PlaySoundFX()
    {
        yield return new WaitForFixedUpdate();
        yield return new WaitForSeconds(0.111f);

        yield return new WaitForSeconds(0.222f);
        airWooshVariants[Random.Range(0, airWooshVariants.Length)].Play();
        yield return new WaitForSeconds(0.0666f);
        metalWooshVariants[Random.Range(0, metalWooshVariants.Length)].Play();

        yield return new WaitForSeconds(0.111f);
        airWooshVariants[Random.Range(0, airWooshVariants.Length)].Play();
        yield return new WaitForSeconds(0.0666f);
        metalWooshVariants[Random.Range(0, metalWooshVariants.Length)].Play();

        //avatarGrunt[Random.Range(0, avatarGrunt.Length)].Play();
        //avatarShout[Random.Range(0, avatarShout.Length)].Play();
    }

    internal void PlayAttackSFX()
    {
        StartCoroutine(PlaySoundFX());
    }
}