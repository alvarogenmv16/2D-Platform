using UnityEngine;

// A static NPC that lifts its head when the player interacts with it while
// standing inside its trigger zone. Reads the Player action's own shared
// InputSystem_Actions instance (via PlayerMovement.InputActions) instead of
// creating a second one, so both stay in sync with the same input state.
[RequireComponent(typeof(Animator))]
public class CompanionInteraction : MonoBehaviour
{
    [SerializeField] private Animator animator;

    [Header("Dialogue")]
    [SerializeField] private DialogueUI dialogueUI;
    // First line is the greeting. Add more here later for a longer
    // conversation - each Interact while the box is open advances one line,
    // and the line after the last one closes it.
    [SerializeField] private string[] lines = { "Hi there! Welcome." };
    [SerializeField] private SfxPlayer sfxPlayer;
    [SerializeField] private AudioClip greetingSound;

    private InputSystem_Actions inputActions;
    private bool playerInRange;

    // -1 means no conversation in progress. 0+ is the index of the line
    // currently shown.
    private int currentLineIndex = -1;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        playerInRange = true;

        if (other.TryGetComponent(out PlayerMovement playerMovement))
        {
            inputActions = playerMovement.InputActions;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        playerInRange = false;

        // Walking away mid-conversation closes the box instead of leaving
        // it stuck open with no one there to advance it.
        EndConversation();
    }

    private void Update()
    {
        if (!playerInRange || inputActions == null) return;

        if (inputActions.Player.Interact.WasPerformedThisFrame())
        {
            HandleInteract();
        }
    }

    private void HandleInteract()
    {
        animator.SetTrigger("InteractTrigger");

        bool isStartingConversation = currentLineIndex == -1;
        currentLineIndex = isStartingConversation ? 0 : currentLineIndex + 1;

        if (currentLineIndex >= lines.Length)
        {
            EndConversation();
            return;
        }

        if (isStartingConversation)
        {
            sfxPlayer?.Play(greetingSound);
        }

        dialogueUI.Show(lines[currentLineIndex]);
    }

    private void EndConversation()
    {
        if (currentLineIndex == -1) return;

        currentLineIndex = -1;
        dialogueUI.Hide();
    }
}
