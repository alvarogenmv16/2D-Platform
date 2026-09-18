using UnityEngine;

// Minimal flying enemy behavior: flies in a straight line toward the player
// whenever they're within range, and stops otherwise. No telegraph, dive or
// attack pattern - unlike FlyingEnemyAI, which drives the full boss-style
// dive attack. Contact damage and death are handled separately (same
// division of responsibility as the ground Walker: BossContactDamage +
// EnemyHealth), so this only ever needs to worry about movement.
[RequireComponent(typeof(FlyingEnemyMovement))]
public class FlyingEnemyChase : MonoBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField] private float detectionRange = 6f;
    [SerializeField] private float moveSpeed = 3f;

    // Child transform holding the sprite. Only THIS gets flipped, never the
    // root (which holds the Rigidbody2D/Collider2D) - same convention as
    // EnemyAI, so flipping never affects physics.
    [SerializeField] private Transform visuals;

    [Header("Sound")]
    [SerializeField] private SfxPlayer sfxPlayer;
    // Plays once, right when the player first enters detection range.
    [SerializeField] private AudioClip awakeSound;
    // Loops while actively chasing; a separate AudioSource (not sfxPlayer)
    // because it needs Play()/Stop() toggling, not a one-shot. Latches on at
    // first detection and keeps playing regardless of range afterward -
    // only stops when EnemyHealth destroys the object on death.
    [SerializeField] private AudioSource flyAudioSource; // Loop on, Play On Awake off
    private bool hasStartedFlying = false;

    private FlyingEnemyMovement movement;
    private float facingDirection = 1f;
    private Vector3 baseVisualsScale;

    // Tracks whether the player was in range on the PREVIOUS FixedUpdate, so
    // the awake sound fires exactly once on the transition, not every frame.
    private bool wasInRangeLastFrame = false;

    // Chasing doesn't start looping until this time is reached - set to
    // "now + awake clip length" whenever awake plays, so the two never
    // overlap. Same pattern as EnemyAI's chargeAllowedTime.
    private float flyLoopAllowedTime = 0f;

    private void Start()
    {
        movement = GetComponent<FlyingEnemyMovement>();

        if (visuals != null)
        {
            baseVisualsScale = visuals.localScale;
        }

        if (player == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
            {
                player = playerObject.transform;
            }
        }
    }

    private void FixedUpdate()
    {
        if (player == null) return;

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);
        bool isInRange = distanceToPlayer <= detectionRange;

        if (isInRange)
        {
            UpdateFacing();
            movement.MoveTowards(player.position, moveSpeed);
        }
        else
        {
            movement.Stop();
        }

        UpdateSound(isInRange);
        wasInRangeLastFrame = isInRange;
    }

    private void UpdateSound(bool isInRange)
    {
        if (isInRange && !wasInRangeLastFrame)
        {
            sfxPlayer?.Play(awakeSound);
            flyLoopAllowedTime = Time.time + (awakeSound != null ? awakeSound.length : 0f);
        }

        if (flyAudioSource == null) return;

        bool shouldLoop = isInRange && Time.time >= flyLoopAllowedTime;

        // Latches on at first detection and keeps playing regardless of
        // range changes afterward - only EnemyHealth destroying the object
        // on death actually stops it.
        if (shouldLoop) hasStartedFlying = true;

        if (hasStartedFlying && !flyAudioSource.isPlaying)
        {
            flyAudioSource.Play();
        }
    }

    private void UpdateFacing()
    {
        float newFacingDirection = Mathf.Sign(player.position.x - transform.position.x);
        if (newFacingDirection == 0f || newFacingDirection == facingDirection) return;

        facingDirection = newFacingDirection;

        if (visuals != null)
        {
            visuals.localScale = new Vector3(
                Mathf.Abs(baseVisualsScale.x) * facingDirection,
                baseVisualsScale.y,
                baseVisualsScale.z
            );
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
    }
}
