using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// A soul floating in the level (soul1/2/3 of save.png, one per world).
// Idles bobbing up and down; when the player touches it, it's counted in
// SoulScore and plays a Mario-star style exit: it spins around its vertical
// axis faster and faster while rising, then shrinks and fades out.
//
// Hierarchy (same root = logic / Visuals = sprite convention as the rest of
// the project, so the spin's scale never touches the trigger collider):
//   Soul (root)  - CircleCollider2D (trigger), AudioSource, SoulCollectible
//     Visuals    - SpriteRenderer
[RequireComponent(typeof(Collider2D))]
public class SoulCollectible : MonoBehaviour
{
    [SerializeField] private Transform visuals;
    [SerializeField] private int points = 1;

    [Header("Idle")]
    [SerializeField] private float bobHeight = 0.15f;
    [SerializeField] private float bobSpeed = 2f;

    [Header("Collect")]
    [SerializeField] private float collectDuration = 0.8f;
    // Turns per second at the start and at the end of the spin.
    [SerializeField] private float startSpinSpeed = 1.5f;
    [SerializeField] private float endSpinSpeed = 6f;
    [SerializeField] private float riseHeight = 1.2f;
    // Last fraction of collectDuration spent shrinking and fading out.
    [SerializeField, Range(0f, 1f)] private float fadePortion = 0.35f;

    [Header("Sound")]
    [SerializeField] private AudioClip collectSound;
    [SerializeField, Range(0f, 1f)] private float collectVolume = 1f;

    private SpriteRenderer spriteRenderer;
    private Vector3 visualsBasePosition;
    private Vector3 visualsBaseScale;
    private float bobOffset;
    private bool collecting;
    private string id;

    private void Awake()
    {
        if (visuals == null && transform.childCount > 0)
        {
            visuals = transform.GetChild(0);
        }

        spriteRenderer = visuals != null ? visuals.GetComponent<SpriteRenderer>() : GetComponentInChildren<SpriteRenderer>();
        visualsBasePosition = visuals.localPosition;
        visualsBaseScale = visuals.localScale;

        // Different phase per soul, so a row of them doesn't bob in sync.
        bobOffset = Random.Range(0f, Mathf.PI * 2f);

        // Identifies this soul across reloads: scene + where it was placed.
        id = $"{SceneManager.GetActiveScene().name}/{transform.position.x:F2},{transform.position.y:F2}";

        GetComponent<Collider2D>().isTrigger = true;
    }

    private void Start()
    {
        // Already taken earlier this run (e.g. the scene reloaded after a death).
        if (SoulScore.IsCollected(id))
        {
            Destroy(gameObject);
        }
    }

    private void Update()
    {
        if (collecting) return;

        float bob = Mathf.Sin(Time.time * bobSpeed + bobOffset) * bobHeight;
        visuals.localPosition = visualsBasePosition + Vector3.up * bob;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (collecting) return;
        if (!other.CompareTag("Player")) return;

        collecting = true;
        GetComponent<Collider2D>().enabled = false;

        SoulScore.Collect(id, points);
        StartCoroutine(CollectSequence());
    }

    private IEnumerator CollectSequence()
    {
        float soundLength = 0f;
        AudioSource audioSource = GetComponent<AudioSource>();

        if (audioSource != null && collectSound != null)
        {
            audioSource.PlayOneShot(collectSound, collectVolume);
            soundLength = collectSound.length;
        }

        Vector3 startPosition = visuals.localPosition;
        Color baseColor = spriteRenderer != null ? spriteRenderer.color : Color.white;
        float angle = 0f; // in turns
        float fadeStart = 1f - fadePortion;

        for (float t = 0f; t < collectDuration; t += Time.deltaTime)
        {
            float k = t / collectDuration;

            // Accelerating spin: scale.x = cos(angle) squashes the sprite to
            // a line and back, and mirrors it on the back half - which reads
            // as a flat object turning around its vertical axis.
            angle += Mathf.Lerp(startSpinSpeed, endSpinSpeed, k) * Time.deltaTime;
            float turn = Mathf.Cos(angle * Mathf.PI * 2f);

            // Ease-out rise.
            float rise = 1f - (1f - k) * (1f - k);

            float fade = k < fadeStart ? 1f : 1f - (k - fadeStart) / fadePortion;

            visuals.localPosition = startPosition + Vector3.up * (riseHeight * rise);
            visuals.localScale = new Vector3(visualsBaseScale.x * turn, visualsBaseScale.y, visualsBaseScale.z) * Mathf.Lerp(0.4f, 1f, fade);

            if (spriteRenderer != null)
            {
                spriteRenderer.color = new Color(baseColor.r, baseColor.g, baseColor.b, baseColor.a * fade);
            }

            yield return null;
        }

        visuals.gameObject.SetActive(false);

        // Destroying the object would also cut the sound short.
        float remaining = soundLength - collectDuration;
        if (remaining > 0f)
        {
            yield return new WaitForSeconds(remaining);
        }

        Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, 0.3f);
    }
}
