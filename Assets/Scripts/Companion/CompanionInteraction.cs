using UnityEngine;

// A static NPC that lifts its head when the player interacts with it while
// standing inside its trigger zone. Reads the Player action's own shared
// InputSystem_Actions instance (via PlayerMovement.InputActions) instead of
// creating a second one, so both stay in sync with the same input state.
[RequireComponent(typeof(Animator))]
public class CompanionInteraction : MonoBehaviour
{
    [SerializeField] private Animator animator;

    // One entry per line of dialogue: its text and the clip that plays the
    // instant it's shown. sound can be left empty for a silent line.
    [System.Serializable]
    private struct DialogueLine
    {
        public string text;
        public AudioClip sound;
    }

    [Header("Dialogue")]
    [SerializeField] private DialogueUI dialogueUI;
    // First entry is the greeting. Each Interact while the box is open
    // advances one line.
    [SerializeField] private DialogueLine[] lines = { new DialogueLine { text = "Hi there! Welcome." } };
    // Shown once the last entry in "lines" has been reached, and stays put -
    // further Interact presses do nothing more. Only walking away (EndConversation)
    // closes the box after this point.
    [SerializeField] private DialogueLine closingLine;
    [SerializeField] private SfxPlayer sfxPlayer;

    private InputSystem_Actions inputActions;
    private bool playerInRange;

    // -1 means no conversation in progress. 0 to lines.Length-1 is the index
    // of the line currently shown. lines.Length means closingLine is shown
    // and the conversation has settled there until the player walks away.
    private int currentLineIndex = -1;

    // Set once the full "lines" sequence has been seen from start to finish.
    // From then on, a fresh conversation skips straight to closingLine instead
    // of replaying the whole greeting again.
    private bool hasCompletedConversation = false;

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
        // Already settled on the closing line: nothing left to advance to,
        // ignore further presses until the player leaves and comes back.
        if (currentLineIndex >= lines.Length) return;

        animator.SetTrigger("InteractTrigger");

        bool isStartingConversation = currentLineIndex == -1;

        if (isStartingConversation && hasCompletedConversation)
        {
            // Already went through the full conversation before: jump
            // straight to the closing line instead of replaying it all.
            currentLineIndex = lines.Length;
        }
        else
        {
            currentLineIndex = isStartingConversation ? 0 : currentLineIndex + 1;
        }

        if (currentLineIndex >= lines.Length)
        {
            hasCompletedConversation = true;
        }

        DialogueLine currentLine = currentLineIndex < lines.Length ? lines[currentLineIndex] : closingLine;
        sfxPlayer?.Play(currentLine.sound);
        dialogueUI.Show(currentLine.text);
    }

    private void EndConversation()
    {
        if (currentLineIndex == -1) return;

        currentLineIndex = -1;
        dialogueUI.Hide();
    }
}
