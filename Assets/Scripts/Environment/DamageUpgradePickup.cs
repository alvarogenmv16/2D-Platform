using UnityEngine;

// A one-time reward: interacting with it while in range shows a line of
// dialogue through the same DialogueUI the Companion uses, and grants a
// permanent damage increase to the player right then - not gated behind a
// second press, the line is flavor for what just happened, not a prompt.
// Reads the Player action's own shared InputSystem_Actions instance (via
// PlayerMovement.InputActions), same convention as CompanionInteraction, so
// both stay in sync with the same input state.
public class DamageUpgradePickup : MonoBehaviour
{
    [Header("Dialogue")]
    [SerializeField] private DialogueUI dialogueUI;
    [SerializeField] private string message = "The relic's power flows into you. Attack damage increased!";
    [SerializeField] private SfxPlayer sfxPlayer;
    [SerializeField] private AudioClip claimSound;

    [Header("Reward")]
    [SerializeField] private float damageIncrease = 1f;

    private InputSystem_Actions inputActions;
    private PlayerAttackHitbox playerAttackHitbox;
    private bool playerInRange;
    private bool hasBeenClaimed = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        playerInRange = true;

        if (other.TryGetComponent(out PlayerMovement playerMovement))
        {
            inputActions = playerMovement.InputActions;
        }

        if (playerAttackHitbox == null)
        {
            playerAttackHitbox = other.GetComponentInChildren<PlayerAttackHitbox>();
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        playerInRange = false;

        // Walking away closes the box, same as CompanionInteraction -
        // nothing left to advance to here since it's a single line, but
        // it shouldn't linger open once the player leaves.
        dialogueUI.Hide();
    }

    private void Update()
    {
        // Already claimed: the trigger and dialogue are done, nothing left
        // for Interact to do. Left enabled (rather than disabling the
        // object) so the pickup can still sit there as a visual marker of
        // "already collected" if its sprite/animation later reflects that.
        if (!playerInRange || hasBeenClaimed || inputActions == null) return;

        if (inputActions.Player.Interact.WasPerformedThisFrame())
        {
            Claim();
        }
    }

    private void Claim()
    {
        hasBeenClaimed = true;

        if (playerAttackHitbox != null)
        {
            playerAttackHitbox.IncreaseDamage(damageIncrease);
        }

        sfxPlayer?.Play(claimSound);
        dialogueUI.Show(message);
    }
}
