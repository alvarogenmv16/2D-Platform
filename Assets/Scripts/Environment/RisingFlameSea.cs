using Unity.Cinemachine;
using UnityEngine;

// A sea of flames that rises up a vertical section once the player takes a
// specific key (triggerKey), forcing a fast climb. It stops for good at
// stopPoint and stays there as a deadly floor.
//
// Getting caught while it rises costs `damage` health, fades to black and
// restarts the section from "just before the key": the player goes back to
// respawnPoint, the sea back to where it started, and the key reappears
// (un-counted in the KeyManager) so taking it again starts the chase over.
// A lethal hit skips the restart - PlayerDeathHandler reloads the scene.
// Before the chase or after it's finished, touching it is just a hazard:
// damage + the usual bump-back to the last safe ground (HazardDamage-style).
//
// The prefab carries its own StopPoint/RespawnPoint children: move them
// where they belong in the scene, their world positions are captured in
// Awake, before the sea starts moving (so they don't rise with it). Their
// position only matters on Y for StopPoint.
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
public class RisingFlameSea : MonoBehaviour
{
    // =========================
    // VARIABLES
    // =========================

    private enum SeaState
    {
        Waiting,   // before the key is taken
        Warning,   // key taken: shake + music, about to rise
        Rising,
        Finished,  // reached stopPoint, stays there for good
        Resetting  // fading to black after catching the player
    }

    [SerializeField] private SeaState currentState = SeaState.Waiting;

    [Header("Trigger")]
    // Taking this key starts the chase. Wire it in the scene (Key3).
    [SerializeField] private KeyBlock triggerKey;
    // Where the player is put back when caught - just before the key.
    [SerializeField] private Transform respawnPoint;

    [Header("Rise")]
    // The sea's base line stops at this height.
    [SerializeField] private Transform stopPoint;
    // Silence between hitting the key and the flames erupting (music +
    // camera shake), so the key's own sound plays on its own first.
    [SerializeField] private float eruptionDelay = 1f;
    // Beat between the eruption and the sea starting to move, so the
    // player hears/feels the warning before having to run.
    [SerializeField] private float warningDelay = 1f;
    [SerializeField] private float riseSpeed = 2.5f;

    [Header("Damage")]
    [SerializeField] private float damage = 2f;

    [Header("Feedback")]
    [SerializeField] private CinemachineImpulseSource impulseSource; // shake when the chase starts
    [SerializeField] private IntroLoopAudio music;
    // When the sea reaches the top: fade the music out, or keep it looping.
    [SerializeField] private bool fadeMusicWhenFinished = true;
    [SerializeField] private float musicFadeDuration = 2f;

    [Header("Layout (local, sea base line = y 0)")]
    [SerializeField] private float width = 30f;
    [SerializeField] private float depth = 40f; // how far the solid body reaches below the base line
    // Top of the hitbox above the base line - kept below the flame tips on
    // purpose, so just brushing a wisp doesn't count as a hit.
    [SerializeField] private float hitboxTop = 2.5f;
    [SerializeField] private SpriteRenderer[] crestLayers; // Draw Mode: Tiled
    [SerializeField] private Transform body; // 1x1 unit sprite, pivot at its top center
    [SerializeField] private float bodyTopOffset = 0.5f; // overlaps the crest's transparent bottom rows
    // Extra width on the crest layers so their sideways sway (FlameSeaWaves)
    // never uncovers their edges.
    [SerializeField] private float crestExtraWidth = 2f;

    private Rigidbody2D rb;
    private Vector2 startPosition;
    private float stopY;
    private Vector2 respawnPosition;
    private float stateTimer;
    private bool hasErupted;

    // =========================
    // START
    // =========================
    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;

        BoxCollider2D hitbox = GetComponent<BoxCollider2D>();
        hitbox.isTrigger = true;

        startPosition = rb.position;
        stopY = stopPoint != null ? stopPoint.position.y : startPosition.y;
        respawnPosition = respawnPoint != null ? (Vector2)respawnPoint.position : startPosition;

        ApplyLayout();
    }

    private void Start()
    {
        if (triggerKey == null)
        {
            Debug.LogWarning($"{name}: RisingFlameSea has no triggerKey, it will never rise.", this);
            return;
        }

        // The key must survive being broken so a restart can put it back.
        triggerKey.KeepAfterBreak();
        triggerKey.OnBroken += HandleKeyBroken;
    }

    private void OnDestroy()
    {
        if (triggerKey != null)
        {
            triggerKey.OnBroken -= HandleKeyBroken;
        }
    }

    // =========================
    // FIXED UPDATE
    // =========================
    private void FixedUpdate()
    {
        switch (currentState)
        {
            case SeaState.Warning:
                stateTimer += Time.fixedDeltaTime;

                if (!hasErupted && stateTimer >= eruptionDelay)
                {
                    Erupt();
                }

                if (stateTimer >= eruptionDelay + warningDelay)
                {
                    currentState = SeaState.Rising;
                }
                break;

            case SeaState.Rising:
                HandleRising();
                break;
        }
    }

    // =========================
    // FUNCTIONS
    // =========================

    private void HandleKeyBroken()
    {
        if (currentState != SeaState.Waiting) return;

        stateTimer = 0f;
        hasErupted = false;
        currentState = SeaState.Warning;
    }

    // The flames "wake up": shake and music land together, eruptionDelay
    // after the key was hit.
    private void Erupt()
    {
        hasErupted = true;

        if (impulseSource != null) impulseSource.GenerateImpulse();
        if (music != null) music.Play();
    }

    private void HandleRising()
    {
        float newY = Mathf.MoveTowards(rb.position.y, stopY, riseSpeed * Time.fixedDeltaTime);
        rb.MovePosition(new Vector2(rb.position.x, newY));

        if (Mathf.Approximately(newY, stopY))
        {
            currentState = SeaState.Finished;

            if (music != null && fadeMusicWhenFinished)
            {
                music.FadeOut(musicFadeDuration);
            }
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (currentState == SeaState.Resetting) return;
        if (!other.TryGetComponent(out PlayerHealth playerHealth)) return;

        Vector2 hitPoint = new Vector2(other.transform.position.x, rb.position.y);

        if (currentState == SeaState.Warning || currentState == SeaState.Rising)
        {
            CatchPlayer(playerHealth, hitPoint);
        }
        else
        {
            // Not chasing: a plain hazard, same as HazardDamage.
            bool damageApplied = playerHealth.TakeDamage(damage, hitPoint);
            if (damageApplied && other.TryGetComponent(out PlayerRespawnPoint safeGround))
            {
                SceneFader.FlashAndTeleport(safeGround);
            }
        }
    }

    private void CatchPlayer(PlayerHealth playerHealth, Vector2 hitPoint)
    {
        currentState = SeaState.Resetting;

        // Even if the hit is ignored (player still invulnerable from an
        // earlier hit), the section restarts - they're inside the flames.
        playerHealth.TakeDamage(damage, hitPoint);

        if (playerHealth.IsDead)
        {
            // PlayerDeathHandler reloads the scene - nothing else to reset.
            if (music != null) music.Stop();
            return;
        }

        GameObject player = playerHealth.gameObject;
        PlayerMovement playerMovement = player.GetComponent<PlayerMovement>();
        Rigidbody2D playerRb = player.GetComponent<Rigidbody2D>();

        if (playerMovement != null) playerMovement.enabled = false;

        SceneFader.FadeOutAndIn(
            atBlack: () => ResetSection(player.transform, playerRb),
            onDone: () =>
            {
                if (playerMovement != null && !playerHealth.IsDead)
                {
                    playerMovement.enabled = true;
                }
            });
    }

    // Runs while the screen is fully black.
    private void ResetSection(Transform player, Rigidbody2D playerRb)
    {
        player.position = respawnPosition;
        if (playerRb != null) playerRb.linearVelocity = Vector2.zero;

        // Teleport, not MovePosition - MovePosition would sweep the trigger
        // back down through everything in between.
        rb.position = startPosition;
        transform.position = startPosition;

        if (music != null) music.Stop();
        if (triggerKey != null) triggerKey.Restore();

        currentState = SeaState.Waiting;
    }

    // Sizes the crest layers, the body and the hitbox from width/depth, so
    // the three always match. Also runs in the Editor (OnValidate).
    private void ApplyLayout()
    {
        if (crestLayers != null)
        {
            foreach (SpriteRenderer crest in crestLayers)
            {
                if (crest != null)
                {
                    crest.size = new Vector2(width + crestExtraWidth, crest.size.y);
                }
            }
        }

        if (body != null)
        {
            body.localPosition = new Vector3(0f, bodyTopOffset, 0f);
            body.localScale = new Vector3(width, depth + bodyTopOffset, 1f);
        }

        BoxCollider2D hitbox = GetComponent<BoxCollider2D>();
        if (hitbox != null)
        {
            hitbox.size = new Vector2(width, depth + hitboxTop);
            hitbox.offset = new Vector2(0f, (hitboxTop - depth) / 2f);
        }
    }

    private void OnValidate()
    {
        ApplyLayout();
    }

    // =========================
    // DEBUG
    // =========================
    private void OnDrawGizmos()
    {
        if (stopPoint != null)
        {
            Gizmos.color = Color.cyan;
            Vector3 stop = new Vector3(transform.position.x, stopPoint.position.y, 0f);
            Gizmos.DrawLine(transform.position, stop);
            Gizmos.DrawLine(stop + Vector3.left * width / 2f, stop + Vector3.right * width / 2f);
        }

        if (respawnPoint != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(respawnPoint.position, 0.4f);
        }
    }
}
