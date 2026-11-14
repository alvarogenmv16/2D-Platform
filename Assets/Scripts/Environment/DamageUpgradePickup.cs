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

    private void Start()
    {
        if (GameProgress.Has(ProgressFlag)) hasBeenClaimed = true;

        // A Prefab Asset can't hold a reference to the scene's DialogueUI, so
        // an instance whose field was never set in the scene would throw on
        // Claim/Hide. Fall back to the scene's one (there's only ever one
        // dialogue box per scene) - found by type, same as SceneFader does
        // for the health/power UI.
        if (dialogueUI == null)
        {
            dialogueUI = FindFirstObjectByType<DialogueUI>(FindObjectsInactive.Include);

            if (dialogueUI == null)
            {
                Debug.LogWarning($"{name}: DamageUpgradePickup found no DialogueUI in the scene - the reward still works, just without the message.", this);
            }
        }
    }

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
        if (dialogueUI != null) dialogueUI.Hide();
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

    // Remembered for the whole run (and in save files) by scene + position,
    // so reloading the level - after a death, or on Continue - doesn't hand
    // the upgrade out a second time to a player who already has it.
    private string ProgressFlag => $"Upgrade:{gameObject.scene.name}/{transform.position.x:F1},{transform.position.y:F1}";

    private void Claim()
    {
        hasBeenClaimed = true;
        GameProgress.Set(ProgressFlag);

        if (playerAttackHitbox != null)
        {
            playerAttackHitbox.IncreaseDamage(damageIncrease);
        }

        // Explicit null checks instead of ?. (Unity fake-null, see KeyBlock).
        if (sfxPlayer != null) sfxPlayer.Play(claimSound);
        if (dialogueUI != null) dialogueUI.Show(message);
    }
}
