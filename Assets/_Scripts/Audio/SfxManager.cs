using System.Collections.Generic;
using UnityEngine;

/// <summary>Every sound effect the game can play (all synthesized at runtime).</summary>
public enum SfxId
{
    None = 0,
    UiClick,       // UI button press
    Tick,          // countdown / round tick
    Whoosh,        // outcome card flies in
    Reveal,        // outcome revealed
    Blank,         // BLANK outcome (soft air)
    Steal,         // zone taken over
    StealFail,     // steal hits a shield
    Boost,         // zone value boosted
    Shield,        // shield applied
    ZoneGain,      // zone flips to a new owner
    ScorePop,      // score text updates
    RoundEnd,      // round finished
    Win,           // game won fanfare
    Draw           // game ends in a draw
}

/// <summary>
/// Runtime synthesized sound effects.
///
/// All SFX are generated procedurally as AudioClips at first use, so the
/// project needs no binary audio assets, ships small, and can never break
/// because of a missing import. Synthesis is intentionally simple (sine /
/// square / saw + noise with pitch envelopes); every clip is cached once and
/// played through a small pooled set of AudioSources.
/// </summary>
public static class Sfx
{
    private const int SampleRate = 44100;

    private static readonly Dictionary<SfxId, AudioClip> _clips = new Dictionary<SfxId, AudioClip>();
    private static AudioSource[] _sources;
    private static GameObject _host;
    private static int _nextSource;

    /// <summary>Master SFX volume (0..1). Persisted between sessions.</summary>
    public static float Volume
    {
        get { return _volume; }
        set
        {
            _volume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat("BonusRoundGame.SfxVolume", _volume);
            PlayerPrefs.Save();
            if (_sources != null)
            {
                for (int i = 0; i < _sources.Length; i++)
                {
                    if (_sources[i] != null) _sources[i].volume = _volume * 0.9f;
                }
            }
        }
    }

    private static float _volume = -1f;

    /// <summary>Loads volume preferences and creates the audio pool.</summary>
    public static void Initialize()
    {
        EnsureReady();
    }

    private static void EnsureReady()
    {
        if (_sources != null && _sources.Length > 0) return;

        if (_host == null)
        {
            _host = new GameObject("[SfxManager]");
            Object.DontDestroyOnLoad(_host);
        }

        if (_volume < 0f)
        {
            _volume = PlayerPrefs.GetFloat("BonusRoundGame.SfxVolume", 1f);
        }

        int poolSize = 8;
        _sources = new AudioSource[poolSize];
        for (int i = 0; i < poolSize; i++)
        {
            AudioSource source = _host.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            source.volume = _volume * 0.9f;
            source.pitch = 1f;
            _sources[i] = source;
        }
    }

    /// <summary>Plays a synthesized effect, optionally with a slight pitch jitter.</summary>
    public static void Play(SfxId id, float pitchJitter = 0f)
    {
        if (id == SfxId.None) return;
        if (_volume < 0f) EnsureReady(); // lazy first-time init
        if (_volume <= 0.01f) return;

        AudioClip clip = GetClip(id);
        if (clip == null) return;

        AudioSource source = _sources[_nextSource];
        _nextSource = (_nextSource + 1) % _sources.Length;

        source.pitch = 1f + Random.Range(-pitchJitter, pitchJitter);
        source.PlayOneShot(clip, 1f);
    }

    private static AudioClip GetClip(SfxId id)
    {
        AudioClip clip;
        if (_clips.TryGetValue(id, out clip)) return clip;
        clip = Synthesize(id);
        if (clip != null) _clips[id] = clip;
        return clip;
    }

    // ------------------------------------------------------------------ synthesis

    private static AudioClip Synthesize(SfxId id)
    {
        switch (id)
        {
            case SfxId.UiClick: return Tone(0.06f, 1800f, 2400f, Wave.Sine, 0.5f, 0.0005f);
            case SfxId.Tick: return Tone(0.055f, 700f, 1100f, Wave.Triangle, 0.35f, 0.0008f);
            case SfxId.Whoosh: return NoiseSweep(0.16f, 0.10f);
            case SfxId.Reveal: return TwoTone(0.10f, 620f, 0.10f, 930f, Wave.Triangle, 0.3f);
            case SfxId.Blank: return Blend(
                NoiseSweep(0.12f, 0.05f),
                Tone(0.14f, 160f, 140f, Wave.Sine, 0.22f, 0.02f));
            case SfxId.Steal: return Blend(
                Sweep(0.18f, 950f, 170f, Wave.Saw, 0.30f),
                NoiseBurst(0.05f, 0.5f));
            case SfxId.StealFail: return Blend(
                Tone(0.14f, 130f, 90f, Wave.Sine, 0.5f, 0.002f),
                NoiseBurst(0.03f, 0.25f));
            case SfxId.Boost: return Blend(
                Sweep(0.16f, 420f, 880f, Wave.Sine, 0.35f),
                Tone(0.10f, 1560f, 1560f, Wave.Sine, 0.15f, 0.12f));
            case SfxId.Shield: return Blend(
                Tone(0.30f, 1050f, 1050f, Wave.Sine, 0.28f, 0.001f),
                Tone(0.22f, 2100f, 2100f, Wave.Sine, 0.12f, 0.001f));
            case SfxId.ZoneGain: return Tone(0.09f, 500f, 880f, Wave.Sine, 0.4f, 0.001f);
            case SfxId.ScorePop: return Tone(0.08f, 660f, 990f, Wave.Triangle, 0.22f, 0.001f);
            case SfxId.RoundEnd: return TwoTone(0.10f, 520f, 0.16f, 392f, Wave.Triangle, 0.25f);
            case SfxId.Win: return Fanfare();
            case SfxId.Draw: return TwoTone(0.14f, 392f, 0.22f, 330f, Wave.Triangle, 0.22f);
            default: return null;
        }
    }

    private enum Wave { Sine, Triangle, Saw, Square }

    private static float Sample(Wave wave, float phase, float t)
    {
        switch (wave)
        {
            case Wave.Sine: return Mathf.Sin(2f * Mathf.PI * phase * t);
            case Wave.Triangle: return 2f / Mathf.PI * Mathf.Asin(Mathf.Sin(2f * Mathf.PI * phase * t));
            case Wave.Saw: return 2f * (phase * t - Mathf.Floor(phase * t + 0.5f));
            case Wave.Square: return Mathf.Sign(Mathf.Sin(2f * Mathf.PI * phase * t));
            default: return 0f;
        }
    }

    private static float Envelope(float t, float attack, float release, float total)
    {
        if (t < attack) return t / Mathf.Max(0.0001f, attack);
        if (t > total - release) return Mathf.Max(0f, (total - t) / Mathf.Max(0.0001f, release));
        return 1f;
    }

    private static AudioClip MakeClip(string name, float[] data)
    {
        AudioClip clip = AudioClip.Create(name, data.Length, 1, SampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    private static AudioClip Tone(float seconds, float f0, float f1, Wave wave, float gain, float attack)
    {
        int n = Mathf.Max(16, Mathf.CeilToInt(seconds * SampleRate));
        float[] data = new float[n];
        for (int i = 0; i < n; i++)
        {
            float t = (float)i / SampleRate;
            float freq = Mathf.Lerp(f0, f1, t / seconds);
            data[i] = Sample(wave, freq, t) * Envelope(t, attack, 0.02f, seconds) * gain;
        }
        return MakeClip("sfx_" + wave + "_" + (int)f0, data);
    }

    private static AudioClip Sweep(float seconds, float f0, float f1, Wave wave, float gain)
    {
        int n = Mathf.CeilToInt(seconds * SampleRate);
        float[] data = new float[n];
        float attack = seconds * 0.05f;
        for (int i = 0; i < n; i++)
        {
            float t = (float)i / SampleRate;
            float freq = Mathf.Lerp(f0, f1, t / seconds);
            data[i] = Sample(wave, freq, t) * Envelope(t, attack, 0.03f, seconds) * gain;
        }
        return MakeClip("sfx_sweep_" + (int)f0, data);
    }

    private static AudioClip TwoTone(float dur1, float f1, float dur2, float f2, Wave wave, float gain)
    {
        int n1 = Mathf.CeilToInt(dur1 * SampleRate);
        int n2 = Mathf.CeilToInt(dur2 * SampleRate);
        float[] data = new float[n1 + n2];
        for (int i = 0; i < n1; i++)
        {
            float t = (float)i / SampleRate;
            data[i] = Sample(wave, f1, t) * Envelope(t, 0.004f, dur1 * 0.4f, dur1) * gain;
        }
        for (int i = 0; i < n2; i++)
        {
            float t = (float)i / SampleRate;
            data[n1 + i] = Sample(wave, f2, t) * Envelope(t, 0.004f, dur2 * 0.5f, dur2) * gain;
        }
        return MakeClip("sfx_twotone_" + (int)f1, data);
    }

    private static AudioClip NoiseBurst(float seconds, float gain)
    {
        int n = Mathf.CeilToInt(seconds * SampleRate);
        float[] data = new float[n];
        for (int i = 0; i < n; i++)
        {
            float t = (float)i / SampleRate;
            data[i] = Random.Range(-1f, 1f) * Envelope(t, 0.002f, seconds * 0.7f, seconds) * gain;
        }
        return MakeClip("sfx_noise_" + (int)(seconds * 1000), data);
    }

    /// <summary>Band-ish noise with a rising 'air' character (used for whooshes).</summary>
    private static AudioClip NoiseSweep(float seconds, float gain)
    {
        int n = Mathf.CeilToInt(seconds * SampleRate);
        float[] data = new float[n];
        float prev = 0f;
        for (int i = 0; i < n; i++)
        {
            float t = (float)i / SampleRate;
            // crude low-passed noise so it reads as "air", not "static"
            prev = Mathf.Lerp(prev, Random.Range(-1f, 1f), 0.25f);
            float swell = Mathf.SmoothStep(0f, 1f, t / seconds);
            data[i] = prev * swell * Envelope(t, 0.004f, seconds * 0.5f, seconds) * gain;
        }
        return MakeClip("sfx_whoosh", data);
    }

    private static AudioClip Fanfare()
    {
        float[] notes = { 523.25f, 659.25f, 783.99f, 1046.5f }; // C5 E5 G5 C6
        float[] durs = { 0.10f, 0.10f, 0.10f, 0.42f };
        float total = 0f;
        for (int i = 0; i < notes.Length; i++) total += durs[i];
        int n = Mathf.CeilToInt(total * SampleRate);
        float[] data = new float[n];
        float cursor = 0f;
        for (int k = 0; k < notes.Length; k++)
        {
            float dur = durs[k];
            int start = Mathf.CeilToInt(cursor * SampleRate);
            int end = Mathf.CeilToInt((cursor + dur) * SampleRate);
            float freq = notes[k];
            for (int i = start; i < end && i < n; i++)
            {
                float t = (float)i / SampleRate;
                float local = t - cursor;
                float env = Envelope(local, 0.008f, dur * (k == notes.Length - 1 ? 0.6f : 0.35f), dur);
                data[i] = (Sample(Wave.Triangle, freq, local) * 0.55f + Sample(Wave.Sine, freq * 2f, local) * 0.15f) * env;
            }
            cursor += dur;
        }
        return MakeClip("sfx_win", data);
    }

    /// <summary>Mixes two same-length clips by adding and soft-clipping.</summary>
    private static AudioClip Blend(AudioClip a, AudioClip b)
    {
        if (a == null) return b;
        if (b == null) return a;
        float[] da = new float[a.samples];
        float[] db = new float[b.samples];
        a.GetData(da, 0);
        b.GetData(db, 0);
        int n = Mathf.Max(da.Length, db.Length);
        float[] mix = new float[n];
        for (int i = 0; i < n; i++)
        {
            float va = i < da.Length ? da[i] : 0f;
            float vb = i < db.Length ? db[i] : 0f;
            mix[i] = (float)System.Math.Tanh((va + vb) * 1.2f) * 0.9f;
        }
        return MakeClip("sfx_blend", mix);
    }
}
