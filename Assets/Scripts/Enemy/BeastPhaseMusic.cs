using System.Collections;
using UnityEngine;

// Morthos' battle music, one looping track per phase (beast1/2/3.wav).
// Lives on the Beast prefab's "Music" child (its own AudioSource, separate
// from the SfxPlayer's), and BeastAI tells it which phase to play: phase 1
// right after the wake-up roar, then the next track each time the boss
// loses another third of its health. Swapping fades the old track out
// first instead of cutting it.
[RequireComponent(typeof(AudioSource))]
public class BeastPhaseMusic : MonoBehaviour
{
    // Index 0 = phase 1, 1 = phase 2, 2 = phase 3.
    [SerializeField] private AudioClip[] phaseMusic;
    // Played once when the boss dies (boss_defeat.wav, same as the other bosses).
    [SerializeField] private AudioClip victoryMusic;
    [SerializeField] private float volume = 0.7f;
    [SerializeField] private float fadeOutTime = 0.6f;

    private AudioSource source;
    private Coroutine swapRoutine;
    private int playingPhase;

    private void Awake()
    {
        source = GetComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = true;
    }

    public void PlayPhase(int phase)
    {
        if (phase == playingPhase) return;
        if (phaseMusic == null || phase < 1 || phase > phaseMusic.Length) return;

        playingPhase = phase;

        if (swapRoutine != null) StopCoroutine(swapRoutine);
        swapRoutine = StartCoroutine(SwapTo(phaseMusic[phase - 1]));
    }

    public void Stop()
    {
        playingPhase = 0;
        if (swapRoutine != null) StopCoroutine(swapRoutine);
        swapRoutine = StartCoroutine(SwapTo(null));
    }

    // Fades the battle track out and plays the victory music once. Detaches
    // from the Beast first: the Beast is destroyed when its death animation
    // ends, and the music has to outlive it.
    public void PlayVictory()
    {
        playingPhase = 0;
        transform.SetParent(null, true);

        if (swapRoutine != null) StopCoroutine(swapRoutine);
        swapRoutine = StartCoroutine(SwapTo(victoryMusic, false));
    }

    private IEnumerator SwapTo(AudioClip clip, bool loop = true)
    {
        if (source.isPlaying && fadeOutTime > 0f)
        {
            float startVolume = source.volume;
            for (float t = 0f; t < fadeOutTime; t += Time.deltaTime)
            {
                source.volume = Mathf.Lerp(startVolume, 0f, t / fadeOutTime);
                yield return null;
            }
        }

        source.Stop();
        if (clip == null) yield break;

        source.clip = clip;
        source.loop = loop;
        source.volume = volume;
        source.Play();
    }
}
