using UnityEngine;

// Thin AudioSource wrapper with pitch variation, so repeated sounds (hits,
// dashes, etc.) get a little natural variety instead of sounding identical
// every single time.
[RequireComponent(typeof(AudioSource))]
public class SfxPlayer : MonoBehaviour
{
    [SerializeField] private float pitchVariation = 0.05f;

    private AudioSource audioSource;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }

    // volumeScale lets each caller balance its own clip (e.g. a loud swing
    // vs. a quieter dash) without needing a separate AudioSource per sound.
    public void Play(AudioClip clip, float volumeScale = 1f)
    {
        if (clip == null) return;

        audioSource.pitch = 1f + Random.Range(-pitchVariation, pitchVariation);
        audioSource.PlayOneShot(clip, volumeScale);
    }
}
