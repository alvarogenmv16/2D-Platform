using System.Collections;
using UnityEngine;

public class PlayerHitReaction : MonoBehaviour
{
    // =========================
    // VARIABLES
    // =========================

    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private Animator animator;

    [Header("Knockback")]
    [SerializeField] private float knockbackForceX = 6f;
    [SerializeField] private float knockbackForceY = 4f;
    [SerializeField] private float hitStunDuration = 0.25f;

    private Rigidbody2D rb;

    // =========================
    // START
    // =========================

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void OnEnable()
    {
        if (playerHealth != null)
        {
            playerHealth.OnDamaged.AddListener(HandleDamaged);
        }
    }

    private void OnDisable()
    {
        if (playerHealth != null)
        {
            playerHealth.OnDamaged.RemoveListener(HandleDamaged);
        }
    }

    // =========================
    // FUNCTIONS
    // =========================

    private void HandleDamaged(float amount, Vector2 hitSourcePosition)
    {
        StartCoroutine(HitReaction(hitSourcePosition));
    }

    private IEnumerator HitReaction(Vector2 hitSourcePosition)
    {
        // Figure out which side the hit came from, ignoring vertical
        // difference, so knockback always pushes purely left/right plus
        // a fixed upward pop, regardless of the attacker's exact height.
        float direction = Mathf.Sign(transform.position.x - hitSourcePosition.x);
        if (direction == 0f) direction = -1f; // fallback if exactly overlapping

        // Disable normal movement so HandleMovement doesn't immediately
        // overwrite the knockback velocity on the very next FixedUpdate.
        if (playerMovement != null)
        {
            playerMovement.enabled = false;
        }

        if (rb != null)
        {
            rb.linearVelocity = new Vector2(direction * knockbackForceX, knockbackForceY);
        }

        if (animator != null)
        {
            animator.SetBool("IsHit", true);
        }

        yield return new WaitForSeconds(hitStunDuration);

        if (animator != null)
        {
            animator.SetBool("IsHit", false);
        }

        if (playerMovement != null)
        {
            playerMovement.enabled = true;
        }
    }
}