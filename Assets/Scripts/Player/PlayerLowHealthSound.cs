using UnityEngine;

// Plays a warning sound (Music/Player/1life.mp3) the moment the
// player is left with lowHealthThreshold health or less (1 life by default).
// It has its own AudioSource, so it can be cut off as soon as the danger is
// over: healing above the threshold, or dying. It only plays again after
// health has gone back above the threshold and dropped once more.
[RequireComponent(typeof(PlayerHealth))]
public class PlayerLowHealthSound : MonoBehaviour
{
    [SerializeField] private AudioClip lowHealthSound;
    [SerializeField, Range(0f, 1f)] private float volume = 1f;
    [SerializeField] private float lowHealthThreshold = 1f;
    // Repeat while still at low health (off = play once).
    [SerializeField] private bool loop = false;

    private PlayerHealth health;
    private AudioSource source;
    private bool isLow;

    private void Awake()
    {
        health = GetComponent<PlayerHealth>();

        source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 0f;
        source.clip = lowHealthSound;
        source.loop = loop;
        source.volume = volume;
    }

    private void OnEnable() => health.OnHealthChanged.AddListener(OnHealthChanged);
    private void OnDisable() => health.OnHealthChanged.RemoveListener(OnHealthChanged);

    private void OnHealthChanged(float current, float max)
    {
        bool low = current > 0f && current <= lowHealthThreshold;

        if (low && !isLow)
        {
            if (lowHealthSound != null) source.Play();
        }
        else if (!low && isLow)
        {
            source.Stop();
        }

        isLow = low;
    }
}
