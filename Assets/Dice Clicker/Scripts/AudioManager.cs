using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Singleton manager responsible for generating and playing sound effects (SFX) throughout the game.
/// Uses procedural audio generation to create simple waveforms and noise-based clips at runtime, eliminating the need for external audio files for basic SFX. 
/// Provides a centralized API for playing various SFX. 
/// Manages an internal pool of AudioSources to allow multiple overlapping SFX without cutting each other off. 
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Settings")]
    [SerializeField] private int initialSfxPoolSize = 6;
    [SerializeField] private int sampleRate = 44100;

    [Range(0f, 1f)]
    [SerializeField] private float masterVolume = 1f;

    private float currentVolume = 1f;
    private bool musicEnabled = false;
    private bool sfxEnabled = true;

    /// <summary>
    /// Basic procedural audio generator for simple waveforms and noise, used to create SFX clips at runtime without needing external audio files.
    /// </summary>
    private static class WaveForm
    {
        public static float Square(float frequency, float time, float dutyCycle = 0.5f) =>
            (Mathf.PingPong(time * frequency, 1f) < dutyCycle) ? 1f : -1f;

        public static float Sawtooth(float frequency, float time) =>
            2f * Mathf.PingPong(time * frequency, 1f) - 1f;

        public static float Triangle(float frequency, float time) =>
            Mathf.PingPong(time * frequency * 2, 2f) - 1f;

        public static float Noise16(float frequency, float time, int seed = 0) =>
            2f * (Mathf.PerlinNoise(time * frequency + seed, 0.5f) - 0.5f);
    }

    private Dictionary<string, AudioClip> _sfxClipCache = new Dictionary<string, AudioClip>();

    private Queue<AudioSource> _sfxPool = new Queue<AudioSource>();

    void Awake()
    {
        if(Instance != null && Instance != this)
        {
            UnityEngine.Debug.LogWarning("Multiple instances of AudioManager detected. Destroying duplicate.", this);
            Destroy(gameObject);
            return;
        }
        Instance = this;

        InitializeSfxPools();
    }

    private void InitializeSfxPools()
    {
        for (int i = 0; i < initialSfxPoolSize; i++)
            _sfxPool.Enqueue(CreateNewSource());
    }

    private AudioSource CreateNewSource()
    {
        GameObject go = new GameObject($"SFXSource_{_sfxPool.Count}");
        go.transform.SetParent(transform);
        var src = go.AddComponent<AudioSource>();
        src.playOnAwake = false;
        src.loop = false;
        src.spatialBlend = 0f; // 2D sound for now
        return src;
    }

    public void SetMasterVolume(float volume)
    {
        masterVolume = Mathf.Clamp01(volume);
        currentVolume = masterVolume;
        // TODO: update save file with  volume setting
    }

    public float GetVolume() => masterVolume;

    #region SFX Playback Generators

    public AudioClip Get_DiceToDiceCollision_SFX()
    {
        const string key = "dicecollision_16bit";
        if (!_sfxClipCache.ContainsKey(key))
        {
            _sfxClipCache[key] = GenerateTone(key, 0.06f, 0.001f, 0.05f, (data, time, env) =>
            {
                // Short dry "clack" — low thump + shaped noise burst
                float t = time / 0.06f;

                // Low thump body (80Hz, dies fast)
                float thump = Mathf.Sin(2f * Mathf.PI * 80f * time) * (1f - t * t);

                // Noise click transient (fades very quickly)
                float click = WaveForm.Noise16(4000f, time, 123);
                click *= Mathf.Max(0f, 1f - t * 6f); // gone by ~16% of duration

                data[0] = (thump * 0.6f + click * 0.4f) * env * 0.55f;
            });
        }
        return _sfxClipCache[key];
    }
    public AudioClip Get_DiceToSurfaceCollision_SFX()
    {
        const string key = "dicefloor_16bit";
        if (!_sfxClipCache.ContainsKey(key))
        {
            _sfxClipCache[key] = GenerateTone(key, 0.1f, 0.001f, 0.08f, (data, time, env) =>
            {
                // Soft dull "thud" — deep sine body + muted noise
                float t = time / 0.1f;

                // Deep body (50Hz sine, drops off quadratically)
                float body = Mathf.Sin(2f * Mathf.PI * 50f * time) * (1f - t * t);

                // Muted surface noise (low freq = darker)
                float surface = WaveForm.Noise16(800f, time, 456) * 0.3f;
                surface *= Mathf.Max(0f, 1f - t * 4f); // fades by 25% of duration

                data[0] = (body * 0.7f + surface * 0.3f) * env * 0.5f;
            });
        }
        return _sfxClipCache[key];
    }

    public AudioClip Get_ShopSuccess_SFX()
    {
        const string key = "shop_success_16bit";
        if (!_sfxClipCache.ContainsKey(key))
        {
            _sfxClipCache[key] = GenerateTone(key, 0.3f, 0.01f, 0.2f, (data, time, env) =>
            {
                float n1 = (time < 0.1f) ? Mathf.Sin(2 * Mathf.PI * 523f * time) : 0f;
                float n2 = (time >= 0.1f && time < 0.2f) ? Mathf.Sin(2 * Mathf.PI * 659f * time) : 0f;
                float n3 = (time >= 0.2f) ? Mathf.Sin(2 * Mathf.PI * 784f * time) : 0f;
                data[0] = (n1 + n2 + n3) * 0.5f * env * 0.5f;
            });
        }
        return _sfxClipCache[key];
    }

    public AudioClip Get_ShopFail_SFX()
    {
        const string key = "shopfail_16bit";
        if (!_sfxClipCache.ContainsKey(key))
        {
            _sfxClipCache[key] = GenerateTone(key, 0.2f, 0.01f, 0.15f, (data, time, env) =>
            {
                float t = time / 0.2f;
                float freq = Mathf.Lerp(220f, 110f, t);
                float saw = WaveForm.Sawtooth(freq, time);
                float sq = WaveForm.Square(freq * 0.5f, time, 0.5f);
                data[0] = (saw * 0.7f + sq * 0.3f) * env * 0.6f;
            });
        }
        return _sfxClipCache[key];
    }

    public AudioClip Get_DiceMerge_SFX()
    {
        const string key = "dicemerge_16bit";
        if (!_sfxClipCache.ContainsKey(key))
        {
            float duration = 0.3f;
            float frequency = 250f;
            _sfxClipCache[key] = GenerateSFXClip(key, duration, (time) =>
            {
                return WaveForm.Square(frequency, time, dutyCycle: 0.3f);
            });
        }
        return _sfxClipCache[key];
    }

    public AudioClip Get_ScoreCounterTick_SFX()
    {
        const string key = "scoretick_16bit";
        if (!_sfxClipCache.ContainsKey(key))
        {
            _sfxClipCache[key] = GenerateTone(key, 0.02f, 0.001f, 0.01f, (data, time, env) =>
            {
                data[0] = Mathf.Sin(2 * Mathf.PI * 1200f * time) * env * 0.4f;
            });
        }
        return _sfxClipCache[key];
    }

    public AudioClip Get_DiceRoll_SFX()
    {
        const string key = "diceroll_16bit";
        if (!_sfxClipCache.ContainsKey(key))
        {
            float duration = 0.12f;
            float attack = 0.003f;
            float decay = 0.09f;

            _sfxClipCache[key] = GenerateTone(key, duration, attack, decay, (data, time, env) =>
            {
                float t = time / duration;

                // Breathy puff — noise-dominant with no tonal pitch
                float air = WaveForm.Noise16(400f, time, 77);
                // Second noise layer at different seed for thickness
                float air2 = WaveForm.Noise16(250f, time, 33) * 0.6f;

                // Shape: fast swell then rapid fade (puff contour)
                float shape = Mathf.Sin(Mathf.PI * t); // peaks at 50% of duration
                shape *= shape; // sharpen the peak

                data[0] = (air + air2) * shape * env * 0.35f;
            });
        }
        return _sfxClipCache[key];
    }

    private AudioClip GenerateTone(string name, float duration, float attackTime, float decayTime, System.Action<float[], float, float> fillCallBack)
    {
        int samples = Mathf.FloorToInt(sampleRate * duration);
        float[] data = new float[samples];
        float[] buf = new float[1]; // persistent mono buffer for callback
        float step = 1f / sampleRate;

        for (int i = 0; i < samples; i++)
        {
            float time = i * step;
            float envelope = 1f;

            // attack phase (linear ramp up)
            if(time < attackTime)
                envelope = time / attackTime;

            // Decay phase (linear ramp down)
            else if(time > duration - decayTime)
                envelope = 1f - (time - (duration - decayTime)) / decayTime;

            fillCallBack(buf, time, envelope);
            data[i] = buf[0];
        }

        AudioClip clip = AudioClip.Create(name, samples, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    private AudioClip GenerateSFXClip(string name, float duration, System.Func<float, float> fillCallBack)
    {
        int samples = Mathf.FloorToInt(sampleRate * duration);
        float[] data = new float[samples];
        float step = 1f / sampleRate;

        for (int i = 0; i < samples; i++)
        {
            float time = i * step;
            data[i] = fillCallBack(time);
        }   
        AudioClip clip = AudioClip.Create(name, samples, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    #endregion


    #region SFX Playback API

    public void PlaySFX_DiceToDiceCollission(float volume = 1f, float pitchVariation = 0.1f, float pitch = 1f)
    {
        PlaySfx(Get_DiceToDiceCollision_SFX(), volume, pitchVariation, pitch);
    }

    public void PlaySFX_DiceToSurfaceCollision(float volume = 1f, float pitchVariation = 0.1f, float pitch = 1f)
    {
        PlaySfx(Get_DiceToSurfaceCollision_SFX(), volume, pitchVariation, pitch);
    }

    public void PlaySFX_DiceRoll(float volume = 1f, float pitchVariation = 0.1f)
    {
        PlaySfx(Get_DiceRoll_SFX(), volume, pitchVariation, 0.5f);
    }

    public void PlaySFX_ShopSuccess(float volume = 1f, float pitchVariation = 0.1f)
    {
        PlaySfx(Get_ShopSuccess_SFX(), volume, pitchVariation);
    }

    public void PlaySFX_DiceMerge(float volume = 1f, float pitchVariation = 0.1f)
    {
        PlaySfx(Get_DiceMerge_SFX(), volume, pitchVariation);
    }

    public void PlaySFX_ScoreCounterTick(float volume = 1f, float pitchVariation = 0.1f)
    {
        PlaySfx(Get_ScoreCounterTick_SFX(), volume, pitchVariation);

    }

    public void PlaySFX_ShopFail(float volume = 1f, float pitchVariation = 0.1f)
    {
        PlaySfx(Get_ShopFail_SFX(), volume, pitchVariation);
    }

    private void PlaySfx(AudioClip clip, float volume, float pitchVariation, float pitch = 1f)
    {
        if (clip == null)
        {
            Debug.LogWarning("AudioManager: Attempted to play a null AudioClip.", this);
            return;
        }

        if (!sfxEnabled)
            return;

        if(_sfxPool.Count == 0)
        {
            Debug.Log("AudioManager: SFX pool exhausted, creating additional source.");
            _sfxPool.Enqueue(CreateNewSource());
        }

        var src = _sfxPool.Dequeue();
        src.pitch = pitch * (1f + Random.Range(-pitchVariation, pitchVariation));
        float finalVolume = currentVolume * volume;
        src.PlayOneShot(clip, finalVolume);

        // Return the source to the pool after the clip finishes
        StartCoroutine(ReturnSourceAfterPlay(src, clip.length / Mathf.Abs(src.pitch)));
    }

    private System.Collections.IEnumerator ReturnSourceAfterPlay(AudioSource src, float duration)
    {
        yield return new WaitForSeconds(duration + 0.05f);
        if (src != null)
            _sfxPool.Enqueue(src);
    }

    public void SetSFXEnabled(bool enabled)
    {
        sfxEnabled = enabled;
    }

    public void SetMusicEnabled(bool enabled)
    {
        musicEnabled = enabled;
    }

    #endregion
}