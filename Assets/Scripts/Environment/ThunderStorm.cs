using UnityEngine;
using System.Collections;
using System.Linq;


[System.Serializable]
public class ThunderStorm : MonoBehaviour
{
    [Tooltip("")]
    public Color32 lightningCollor;

    [Tooltip("")]
    [Range(1, 9)]
    public int minimumBurst = 3;

    [Range(1, 9)]
    [Tooltip("")]
    public int maximumBurst = 7;

    [Range(1, 60)]
    [Tooltip("")]
    public float delayBetweenBursts = 3f;

    [Range(1, 12)]
    [Tooltip("")]
    public float delayBetweenLightnings = 0.111f;

    [Tooltip("")]
    public Light[] currentLightningPool;

    [Tooltip("")]
    private float _nextBurstSchedule;

    [Header("SFX Priority")]
    [Space(1)]
    [Range(0, 256)]
    public int sfxMasterPriority = 180;
    [Range(0, 1)]
    public float sfxThunderClapPriority = 0.999f;
    [Range(0, 1)]
    public float sfxThunderGrowlPriority = 0.666f;
    [Space(2)]


    [Header("SFX Volume")]
    [Space(1)]
    [Range(0, 1)]
    public float sfxMasterVolume = 1f;
    [Range(0, 1)]
    public float sfxThunderClapVolume = 0.999f;
    [Range(0, 1)]
    public float sfxThunderGrowlVolume = 0.666f;
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
    private AudioSource[] thunderClapVariants;
    [SerializeField]
    private AudioSource[] thunderGrownVariants;


    private void Start()
    {
        StartCoroutine(SetUpLightningStorm());
    }

    private IEnumerator SetUpLightningStorm()
    {
        yield return new WaitForFixedUpdate();

        currentLightningPool = new Light[] { };

        for (var i = 0; i < maximumBurst; i++)
        {
            var _newLightningSource = new GameObject().AddComponent<Light>();
            _newLightningSource.transform.SetParent(transform);
            _newLightningSource.transform.localPosition = (Vector3.right * i) + (Vector3.forward * i);
            _newLightningSource.bounceIntensity = 8f;
            _newLightningSource.intensity = 0f;
            _newLightningSource.shadows = LightShadows.Soft;
            _newLightningSource.type = LightType.Directional;
            _newLightningSource.color = lightningCollor;
        }

        currentLightningPool
        = GetComponentsInChildren<Light>();

        thunderClapVariants = GetComponents<AudioSource>().
            Where(x => x.clip.name.Contains("ThunderClap")).ToArray();

        thunderGrownVariants = GetComponents<AudioSource>().
            Where(x => x.clip.name.Contains("ThunderGrowl")).ToArray();

        foreach (var _sndSource in thunderClapVariants)
        {
            _sndSource.playOnAwake = false;
            _sndSource.loop = false;
            _sndSource.priority = Mathf.RoundToInt(sfxThunderClapPriority * sfxMasterPriority);
            _sndSource.volume = sfxThunderClapVolume * sfxMasterVolume;
            _sndSource.spatialBlend = sfxSpatialBlend;
            _sndSource.spread = sfxSpread;
        }

        foreach (var _sndSource in thunderGrownVariants)
        {
            _sndSource.playOnAwake = false;
            _sndSource.loop = false;
            _sndSource.priority = Mathf.RoundToInt(sfxThunderGrowlPriority * sfxMasterPriority);
            _sndSource.volume = sfxThunderGrowlVolume * sfxMasterVolume;
            _sndSource.spatialBlend = sfxSpatialBlend;
            _sndSource.spread = sfxSpread;
            _sndSource.maxDistance = sfxMaxDistance;
        }

        yield return new WaitForSeconds(3f);
        StartCoroutine(FadeLightningStorm());
    }


    private IEnumerator FadeLightningStorm()
    {
        while (true)
        {
            yield return new WaitForFixedUpdate();
            var _isDark = true;
            for (var i = 0; i < currentLightningPool.Length; i++)
            {
                currentLightningPool[i].intensity
                = Mathf.Lerp(currentLightningPool[i].intensity, 0f, Time.deltaTime);
            }

            for (var i = 0; i < currentLightningPool.Length; i++)
            {
                _isDark = currentLightningPool[i].intensity < 0.0666f;
                if (!_isDark)
                {
                    break;
                }
            }

            if (_isDark && Time.time > _nextBurstSchedule)
            {
                yield return StartCoroutine(NewLightningStorm());
            }
        }
    }


    private IEnumerator NewLightningStorm()
    {
        var _newBurstDuration = 0f;
        for (var i = 0; i < Random.Range(minimumBurst, maximumBurst); i++)
        {
            _newBurstDuration += Random.Range(delayBetweenLightnings / 3f, delayBetweenLightnings) + i * 0.333f;
            StartCoroutine(NewLightning(_newBurstDuration, i));
        }

        _nextBurstSchedule
        = Time.time + _newBurstDuration
                + Random.Range(delayBetweenBursts / 3, delayBetweenBursts);

        yield return new WaitForEndOfFrame();
    }


    private IEnumerator NewLightning(float _delay, int _index)
    {
        yield return new WaitForSeconds(_delay);
        currentLightningPool[_index].transform.rotation = Quaternion.identity;

        currentLightningPool[_index].transform.
            Rotate(Random.Range(0f, 180f), Random.Range(-180, 180f), 0f);

        currentLightningPool[_index].intensity 
            = Random.Range(2f, 6f);

        StartCoroutine(ThunderClapSFX());
        StartCoroutine(ThunderGrowlSFX());
    }

    private IEnumerator ThunderClapSFX()
    {
        yield return new WaitForFixedUpdate();
        int _randomThunderClapSFX
            = Random.Range(0, thunderClapVariants.Length);

        thunderClapVariants[_randomThunderClapSFX].pitch 
            = Random.Range(0.88f,1.11f);

        thunderClapVariants[_randomThunderClapSFX].Play();
    }

    private IEnumerator ThunderGrowlSFX()
    {
        yield return new WaitForSeconds(Random.Range(0.333f,0.999f));

        int _randomThunderGrowlSFX
            = Random.Range(0, thunderClapVariants.Length);

        thunderClapVariants[_randomThunderGrowlSFX].pitch 
            = Random.Range(0.88f, 1.11f);

        thunderClapVariants[_randomThunderGrowlSFX].Play();
    }
}
