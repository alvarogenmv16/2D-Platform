using System.Collections;
using UnityEngine;

// Plays a clip once from the start and then loops only its tail, forever:
// 0 -> loopStartTime plays once as an intro, then loopStartTime -> end
// repeats seamlessly. Unity's AudioSource can only loop a whole clip, so
// the tail is copied into its own clip at runtime and played on a second
// AudioSource, scheduled on the DSP clock to start on the exact sample the
// intro ends - no gap and no frame-timing jitter at the seam.
//
// The clip's import settings must be Load Type: Decompress On Load (the
// default) - GetData can't read a streamed clip. If building the tail
// fails, it falls back to looping the whole clip.
public class IntroLoopAudio : MonoBehaviour
{
    [SerializeField] private AudioClip clip;
    [SerializeField] private float loopStartTime = 7f; // seconds into the clip where the loop begins
    [SerializeField, Range(0f, 1f)] private float volume = 1f;

    private AudioSource introSource;
    private AudioSource loopSource;
    private AudioClip loopClip;
    private Coroutine playRoutine;
    private Coroutine fadeRoutine;

    public bool IsPlaying { get; private set; }

    private void Awake()
    {
        introSource = CreateSource();
        loopSource = CreateSource();
    }

    private AudioSource CreateSource()
    {
        AudioSource source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 0f; // 2D: music, not a positional sound
        return source;
    }

    public void Play()
    {
        if (IsPlaying || clip == null) return;

        IsPlaying = true;
        StopFade();
        playRoutine = StartCoroutine(PlayRoutine());
    }

    public void Stop()
    {
        IsPlaying = false;
        StopFade();

        if (playRoutine != null)
        {
            StopCoroutine(playRoutine);
            playRoutine = null;
        }

        introSource.Stop();
        loopSource.Stop();
        introSource.volume = volume;
        loopSource.volume = volume;
    }

    public void FadeOut(float duration)
    {
        if (!IsPlaying) return;

        StopFade();
        fadeRoutine = StartCoroutine(FadeOutRoutine(duration));
    }

    private IEnumerator PlayRoutine()
    {
        // Preload Audio Data may be off on the clip: load it first, or
        // GetData/playback would start on an empty clip.
        if (clip.loadState != AudioDataLoadState.Loaded)
        {
            clip.LoadAudioData();
            while (clip.loadState == AudioDataLoadState.Loading) yield return null;
        }

        if (loopClip == null)
        {
            loopClip = BuildLoopClip();
        }

        introSource.clip = clip;
        introSource.volume = volume;
        loopSource.volume = volume;

        // A small lead time so both sources are queued before the start.
        double startTime = AudioSettings.dspTime + 0.1;

        if (loopClip != null)
        {
            introSource.loop = false;
            introSource.PlayScheduled(startTime);
            introSource.SetScheduledEndTime(startTime + loopStartTime);

            loopSource.clip = loopClip;
            loopSource.loop = true;
            loopSource.PlayScheduled(startTime + loopStartTime);
        }
        else
        {
            Debug.LogWarning($"{name}: IntroLoopAudio couldn't build the loop part of '{clip.name}' (is its Load Type 'Decompress On Load'?). Looping the whole clip instead.", this);
            introSource.loop = true;
            introSource.PlayScheduled(startTime);
        }

        playRoutine = null;
    }

    private AudioClip BuildLoopClip()
    {
        int startSample = Mathf.RoundToInt(loopStartTime * clip.frequency);
        if (startSample <= 0 || startSample >= clip.samples) return null;

        int length = clip.samples - startSample;
        float[] data = new float[length * clip.channels];
        if (!clip.GetData(data, startSample)) return null;

        AudioClip tail = AudioClip.Create($"{clip.name}_loop", length, clip.channels, clip.frequency, false);
        tail.SetData(data, 0);
        return tail;
    }

    private IEnumerator FadeOutRoutine(float duration)
    {
        float startVolume = volume;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float current = Mathf.Lerp(startVolume, 0f, elapsed / duration);
            introSource.volume = current;
            loopSource.volume = current;
            yield return null;
        }

        fadeRoutine = null;
        Stop();
    }

    private void StopFade()
    {
        if (fadeRoutine != null)
        {
            StopCoroutine(fadeRoutine);
            fadeRoutine = null;
        }
    }

    private void OnDestroy()
    {
        if (loopClip != null)
        {
            Destroy(loopClip);
        }
    }
}
