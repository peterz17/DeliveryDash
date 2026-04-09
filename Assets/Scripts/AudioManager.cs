using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    float masterVolume = 1f;
    float bgmVolume = 1f;
    float sfxVolume = 1f;

    AudioSource sfxSrc;
    AudioSource bgmSrc;

    AudioClip clipPickup, clipDelivered, clipWrong, clipTimerWarn, clipGameOver, clipCrash;
    AudioClip clipButtonClick, clipTierUp, clipBossArrive;
    AudioClip clipPowerUpShield, clipPowerUpRocket, clipPowerUpClock;
    AudioClip bgmNormal, bgmExtreme;

    // ── note frequencies (Hz) ────────────────────────────────────────────────
    const float C3 = 130.81f, D3 = 146.83f, E3 = 164.81f, F3 = 174.61f, G3 = 196.00f, A3 = 220.00f, B3 = 246.94f;
    const float C4 = 261.63f, D4 = 293.66f, E4 = 329.63f, F4 = 349.23f;
    const float G4 = 392.00f, A4 = 440.00f, B4 = 493.88f;
    const float C5 = 523.25f, D5 = 587.33f, E5 = 659.25f, F5 = 698.46f;
    const float G5 = 783.99f, A5 = 880.00f, B5 = 987.77f, C6 = 1046.50f;

    public static void Play(System.Action<AudioManager> action)
    {
        if (Instance != null) action(Instance);
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        masterVolume = PlayerPrefs.GetFloat("masterVolume", 1f);
        bgmVolume = PlayerPrefs.GetFloat("bgmVolume", 1f);
        sfxVolume = PlayerPrefs.GetFloat("sfxVolume", 1f);

        sfxSrc = gameObject.AddComponent<AudioSource>();
        sfxSrc.playOnAwake = false;

        bgmSrc = gameObject.AddComponent<AudioSource>();
        bgmSrc.playOnAwake = false;
        bgmSrc.loop   = true;
        bgmSrc.volume = 0.32f * bgmVolume * masterVolume;
        bgmNormal  = BuildBGM();
        bgmExtreme = BuildExtremeBGM();
        bgmSrc.clip = bgmNormal;

        clipPickup    = GenSweep(0.12f, 380f, 820f, 0.30f);
        clipDelivered = GenArpeggio(new[] { 523f, 659f, 784f, 1047f }, 0.08f, 0.40f);
        clipWrong     = GenSweep(0.28f, 260f, 110f, 0.25f);
        clipTimerWarn = GenBeep(1040f, 0.09f, 0.45f);
        clipGameOver  = GenArpeggio(new[] { 392f, 330f, 262f, 196f }, 0.20f, 0.35f);
        clipCrash     = GenCrash();

        clipButtonClick  = GenBeep(480f, 0.07f, 0.28f);
        clipTierUp       = GenArpeggio(new[] { C5, E5, G5, C6 }, 0.09f, 0.45f);
        clipBossArrive   = GenArpeggio(new[] { 370f, 330f, 294f }, 0.14f, 0.60f);
        clipPowerUpShield = GenSweep(0.18f, 520f, 880f, 0.40f);
        clipPowerUpRocket = GenSweep(0.15f, 340f, 920f, 0.42f);
        clipPowerUpClock  = GenArpeggio(new[] { G5, E5, C5 }, 0.09f, 0.38f);
    }

    // ── SFX ─────────────────────────────────────────────────────────────────
    void PlaySFX(AudioClip clip, float baseVol)
    {
        if (sfxSrc != null && clip != null)
            sfxSrc.PlayOneShot(clip, baseVol * sfxVolume * masterVolume);
    }

    public void PlayPickup()      => PlaySFX(clipPickup,    0.80f);
    public void PlayDelivered()   => PlaySFX(clipDelivered, 0.90f);
    public void PlayWrong()       => PlaySFX(clipWrong,     0.70f);
    public void PlayTimerWarn()   => PlaySFX(clipTimerWarn, 0.60f);
    public void PlayGameOver()    => PlaySFX(clipGameOver,  0.80f);
    public void PlayCrash()       => PlaySFX(clipCrash,     1.00f);
    public void PlayButtonClick() => PlaySFX(clipButtonClick, 0.55f);
    public void PlayTierUp()      => PlaySFX(clipTierUp,      0.80f);
    public void PlayBossArrive()  => PlaySFX(clipBossArrive,  0.85f);

    public void PlayPowerUpPickup(PowerUpType type)
    {
        var clip = type switch
        {
            PowerUpType.Shield => clipPowerUpShield,
            PowerUpType.Rocket => clipPowerUpRocket,
            PowerUpType.Clock  => clipPowerUpClock,
            _                  => null
        };
        PlaySFX(clip, 0.75f);
    }

    public float MasterVolume => masterVolume;
    public float BGMVolume => bgmVolume;
    public float SFXVolume => sfxVolume;

    public void SetMasterVolume(float v) => SetVolume(ref masterVolume, "masterVolume", v, true);
    public void SetBGMVolume(float v)    => SetVolume(ref bgmVolume,    "bgmVolume",    v, true);
    public void SetSFXVolume(float v)    => SetVolume(ref sfxVolume,    "sfxVolume",    v, false);

    void SetVolume(ref float field, string prefsKey, float value, bool updateBGM)
    {
        field = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(prefsKey, field);
        if (updateBGM && bgmSrc != null)
            bgmSrc.volume = 0.32f * bgmVolume * masterVolume;
    }

    // ── BGM ──────────────────────────────────────────────────────────────────
    public void PlayBGM()
    {
        if (bgmSrc == null) return;
        bool extreme = GameManager.Instance != null && GameManager.Instance.IsExtreme;
        var targetClip = extreme ? bgmExtreme : bgmNormal;
        if (bgmSrc.clip != targetClip) { bgmSrc.clip = targetClip; bgmSrc.time = 0f; }
        if (!bgmSrc.isPlaying) bgmSrc.Play();
    }
    public void PauseBGM()  { if (bgmSrc != null) bgmSrc.Pause(); }
    public void ResumeBGM() { if (bgmSrc != null) bgmSrc.UnPause(); }
    public void StopBGM()   { if (bgmSrc != null) { bgmSrc.Stop(); bgmSrc.time = 0f; } }

    // ── Piano tone helper ───────────────────────────────────────────────────
    static void AddPianoNote(float[] data, int startSample, float freq, float duration, int sr, float vol, float decay = 4f)
    {
        if (freq < 1f) return;
        int samples = (int)(sr * duration);
        for (int i = 0; i < samples && startSample + i < data.Length; i++)
        {
            float t = (float)i / sr;
            float env = Mathf.Exp(-decay * t);
            float v = Mathf.Sin(freq * t * Mathf.PI * 2f)
                    + Mathf.Sin(2f * freq * t * Mathf.PI * 2f) * 0.5f * Mathf.Exp(-6f * t)
                    + Mathf.Sin(3f * freq * t * Mathf.PI * 2f) * 0.25f * Mathf.Exp(-8f * t)
                    + Mathf.Sin(4f * freq * t * Mathf.PI * 2f) * 0.12f * Mathf.Exp(-10f * t);
            data[startSample + i] += v * env * vol;
        }
    }

    // ── BGM generator — gentle piano, C major ───────────────────────────────
    AudioClip BuildBGM()
    {
        const int SR = 44100;
        const float BPM = 108f;
        const float BEAT = 60f / BPM;

        // Right hand melody
        (float hz, float b)[] mel =
        {
            (E5,1f),(G5,0.5f),(E5,0.5f),(D5,1f),(C5,1f),
            (E5,0.5f),(F5,0.5f),(G5,1f),(A5,1f),(G5,1f),
            (F5,0.5f),(E5,0.5f),(D5,1f),(E5,1f),(C5,1f),
            (D5,0.5f),(E5,0.5f),(D5,1f),(C5,1f),(0f,1f),
            (G5,1f),(A5,0.5f),(G5,0.5f),(E5,1f),(C5,1f),
            (D5,0.5f),(E5,0.5f),(F5,1f),(E5,1f),(D5,1f),
            (C5,0.5f),(D5,0.5f),(E5,1f),(G4,1f),(A4,1f),
            (C5,2f),(0f,1f),(C5,1f),
        };

        // Left hand — arpeggiated chords
        (float hz, float b)[] left =
        {
            (C3,0.5f),(E3,0.5f),(G3,0.5f),(C4,0.5f),(G3,0.5f),(E3,0.5f),(C3,0.5f),(G3,0.5f),
            (A3,0.5f),(C4,0.5f),(E4,0.5f),(A3,0.5f),(F3,0.5f),(A3,0.5f),(C4,0.5f),(F3,0.5f),
            (G3,0.5f),(B3,0.5f),(D4,0.5f),(G3,0.5f),(C3,0.5f),(E3,0.5f),(G3,0.5f),(C4,0.5f),
            (F3,0.5f),(A3,0.5f),(C4,0.5f),(F3,0.5f),(G3,0.5f),(B3,0.5f),(D4,0.5f),(G3,0.5f),
            (C3,0.5f),(E3,0.5f),(G3,0.5f),(C4,0.5f),(E3,0.5f),(G3,0.5f),(C4,0.5f),(E3,0.5f),
            (F3,0.5f),(A3,0.5f),(C4,0.5f),(F3,0.5f),(G3,0.5f),(B3,0.5f),(D4,0.5f),(G3,0.5f),
            (A3,0.5f),(C4,0.5f),(E4,0.5f),(A3,0.5f),(F3,0.5f),(A3,0.5f),(G3,0.5f),(B3,0.5f),
            (C3,0.5f),(G3,0.5f),(E3,0.5f),(G3,0.5f),(C3,0.5f),(E3,0.5f),(G3,0.5f),(C4,0.5f),
        };

        float[] data = RenderTracks(mel, left, SR, BEAT, 0.18f, 3.5f, 1.5f, 0.10f, 5f, 2f);
        return FinalizeBGM(data, data.Length, SR, "bgm_piano");
    }

    // ── Extreme BGM — fast dramatic piano, D minor ─────────────────────────
    AudioClip BuildExtremeBGM()
    {
        const int SR = 44100;
        const float BPM = 145f;
        const float BEAT = 60f / BPM;

        float Bb4x = 466.16f, Bb5x = 932.33f;
        float Dm5 = 587.33f, Dm4 = 293.66f;

        (float hz, float b)[] mel =
        {
            (D5,0.25f),(F5,0.25f),(A5,0.25f),(D5,0.25f),(F5,0.5f),(E5,0.5f),(D5,1f),
            (Bb4x,0.25f),(D5,0.25f),(F5,0.25f),(A5,0.25f),(G5,0.5f),(F5,0.5f),(E5,1f),
            (A5,0.25f),(G5,0.25f),(F5,0.25f),(E5,0.25f),(D5,0.25f),(E5,0.25f),(F5,0.25f),(G5,0.25f),
            (A5,1f),(G5,0.5f),(F5,0.5f),(E5,0.5f),(D5,0.5f),
            (D5,0.25f),(A5,0.25f),(D5,0.25f),(A5,0.25f),(F5,0.25f),(A5,0.25f),(F5,0.25f),(A5,0.25f),
            (G5,0.25f),(Bb5x,0.25f),(G5,0.25f),(Bb5x,0.25f),(A5,1f),(0f,1f),
            (F5,0.25f),(E5,0.25f),(D5,0.25f),(C5,0.25f),(D5,0.25f),(E5,0.25f),(F5,0.25f),(A5,0.25f),
            (G5,0.5f),(F5,0.5f),(E5,0.5f),(D5,0.5f),(Dm4,1f),(0f,1f),
        };

        float D2 = 73.42f, A2 = 110f, Bb2x = 116.54f, G2 = 98f, F2 = 87.31f;
        float D3x = 146.83f, A3x = 220f, F3x = 174.61f, G3x = 196f;
        (float hz, float b)[] left =
        {
            (D2,0.25f),(D3x,0.25f),(A2,0.25f),(D3x,0.25f),(D2,0.25f),(F3x,0.25f),(A2,0.25f),(F3x,0.25f),
            (Bb2x,0.25f),(F3x,0.25f),(D3x,0.25f),(F3x,0.25f),(Bb2x,0.25f),(D3x,0.25f),(A2,0.25f),(D3x,0.25f),
            (G2,0.25f),(G3x,0.25f),(Bb2x,0.25f),(G3x,0.25f),(A2,0.25f),(A3x,0.25f),(G2,0.25f),(A3x,0.25f),
            (D2,0.25f),(A2,0.25f),(D3x,0.25f),(A2,0.25f),(D2,0.5f),(A2,0.5f),(D3x,1f),
            (D2,0.25f),(D3x,0.25f),(A2,0.25f),(D3x,0.25f),(F2,0.25f),(F3x,0.25f),(A2,0.25f),(F3x,0.25f),
            (G2,0.25f),(G3x,0.25f),(Bb2x,0.25f),(G3x,0.25f),(A2,0.5f),(D3x,0.5f),(A2,1f),
            (D2,0.25f),(A2,0.25f),(D3x,0.25f),(F3x,0.25f),(A2,0.25f),(D3x,0.25f),(A2,0.25f),(F3x,0.25f),
            (G2,0.25f),(Bb2x,0.25f),(A2,0.25f),(D3x,0.25f),(D2,0.5f),(D3x,0.5f),(D2,1f),
        };

        float[] data = RenderTracks(mel, left, SR, BEAT, 0.22f, 5f, 1.2f, 0.13f, 6f, 1.8f);
        return FinalizeBGM(data, data.Length, SR, "bgm_extreme_piano");
    }

    static float[] RenderTracks((float hz, float b)[] melody, (float hz, float b)[] bass,
        int sr, float beat, float melVol, float melDecay, float melSustain,
        float bassVol, float bassDecay, float bassSustain)
    {
        float totalBeats = 0f;
        foreach (var n in melody) totalBeats += n.b;
        int total = (int)(sr * beat * totalBeats);
        float[] data = new float[total];

        int pos = 0;
        foreach (var (freq, beats) in melody)
        {
            int ns = (int)(sr * beat * beats);
            AddPianoNote(data, pos, freq, beat * beats * melSustain, sr, melVol, melDecay);
            pos += ns;
        }

        pos = 0;
        foreach (var (freq, beats) in bass)
        {
            int ns = (int)(sr * beat * beats);
            AddPianoNote(data, pos, freq, beat * beats * bassSustain, sr, bassVol, bassDecay);
            pos += ns;
        }

        return data;
    }

    static AudioClip FinalizeBGM(float[] data, int total, int sr, string name)
    {
        int tail = Mathf.Min(4096, total);
        for (int i = 0; i < tail; i++) data[total - tail + i] *= 1f - (float)i / tail;

        float peak = 0f;
        foreach (float s in data) peak = Mathf.Max(peak, Mathf.Abs(s));
        if (peak > 0.85f) { float sc = 0.85f / peak; for (int i = 0; i < data.Length; i++) data[i] *= sc; }

        var clip = AudioClip.Create(name, total, 1, sr, false);
        clip.SetData(data, 0);
        return clip;
    }

    // ── SFX primitives ───────────────────────────────────────────────────────
    static AudioClip GenSweep(float duration, float startHz, float endHz, float vol)
    {
        const int sr = 44100;
        int n = (int)(sr * duration);
        float[] d = new float[n];
        float phase = 0f;
        for (int i = 0; i < n; i++)
        {
            float t = (float)i / n;
            float freq = Mathf.Lerp(startHz, endHz, t);
            phase += freq / sr;
            d[i] = Mathf.Sin(phase * Mathf.PI * 2f) * Mathf.Sin(t * Mathf.PI) * vol;
        }
        var c = AudioClip.Create("sweep", n, 1, sr, false);
        c.SetData(d, 0);
        return c;
    }

    static AudioClip GenArpeggio(float[] freqs, float noteLen, float vol)
    {
        const int sr = 44100;
        int ns = (int)(sr * noteLen);
        float[] d = new float[ns * freqs.Length];
        for (int ni = 0; ni < freqs.Length; ni++)
        {
            float freq = freqs[ni];
            for (int i = 0; i < ns; i++)
            {
                float t = (float)i / ns;
                d[ni * ns + i] = Mathf.Sin(i * freq / sr * Mathf.PI * 2f) * Mathf.Sin(t * Mathf.PI) * vol;
            }
        }
        var c = AudioClip.Create("arp", d.Length, 1, sr, false);
        c.SetData(d, 0);
        return c;
    }

    static AudioClip GenBeep(float hz, float duration, float vol)
    {
        const int sr = 44100;
        int n = (int)(sr * duration);
        float[] d = new float[n];
        for (int i = 0; i < n; i++)
        {
            float t = (float)i / n;
            float env = t < 0.15f ? t / 0.15f : (t > 0.75f ? (1f - t) / 0.25f : 1f);
            d[i] = Mathf.Sin(i * hz / sr * Mathf.PI * 2f) * env * vol;
        }
        var c = AudioClip.Create("beep", n, 1, sr, false);
        c.SetData(d, 0);
        return c;
    }

    static AudioClip GenCrash()
    {
        const int sr = 44100;
        int n = (int)(sr * 0.38f);
        float[] d = new float[n];
        for (int i = 0; i < n; i++)
        {
            float t   = (float)i / n;
            float env = Mathf.Pow(1f - t, 1.8f);
            float thud = Mathf.Sin(i * 75f / sr * Mathf.PI * 2f) * 0.6f;
            float noise = Mathf.Sin(i * 3741.7f / sr * Mathf.PI * 2f)
                        + Mathf.Sin(i * 2193.1f / sr * Mathf.PI * 2f) * 0.8f
                        + Mathf.Sin(i * 5917.3f / sr * Mathf.PI * 2f) * 0.6f;
            noise = Mathf.Clamp(noise * 0.5f, -1f, 1f);
            d[i] = (thud * 0.4f + noise * 0.6f) * env * 0.85f;
        }
        var c = AudioClip.Create("crash", n, 1, sr, false);
        c.SetData(d, 0);
        return c;
    }
}
