using UnityEngine;

// Lets the player walk through an open BossDoor into another scene: while
// standing in this trigger zone with the door open, a prompt ("Enter
// temple") shows in the scene's DialogueUI, and Interact (E / gamepad north,
// a Hold action) fades into nextSceneName, landing on the LevelEntryPoint
// with entryPointId - same SceneFader transition LevelExit uses, just
// started by a button press instead of walking into it.
//
// Setup: on a child of the door with a trigger Collider2D covering the
// doorway (BossDoor's own solid collider is disabled once it opens, so it
// can't be the zone). The DialogueUI is found by type if left empty - a
// Prefab Asset can't reference the scene's one (same as DamageUpgradePickup).
[RequireComponent(typeof(Collider2D))]
public class DoorEntrance : MonoBehaviour
{
    [SerializeField] private BossDoor door; // found in parents if left empty

    [Header("Destination")]
    [SerializeField] private string nextSceneName = "TempleScene";
    [SerializeField] private string entryPointId = "TempleEntry";

    [Header("Prompt")]
    [SerializeField] private DialogueUI dialogueUI;
    [SerializeField] private string promptMessage = "Enter temple";

    [Header("Sound (optional)")]
    [SerializeField] private SfxPlayer sfxPlayer;
    [SerializeField] private AudioClip enterSound;

    private InputSystem_Actions inputActions;
    private bool playerInRange;
    private bool isPromptShown;
    private bool hasEntered;

    private void Awake()
    {
        GetComponent<Collider2D>().isTrigger = true;

        if (door == null)
        {
            door = GetComponentInParent<BossDoor>();
        }
    }

    private void Start()
    {
        if (dialogueUI == null)
        {
            dialogueUI = FindFirstObjectByType<DialogueUI>(FindObjectsInactive.Include);
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
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        playerInRange = false;
        SetPrompt(false);
    }

    private void Update()
    {
        if (hasEntered) return;

        // Checked every frame (not only on enter): the door can open while
        // the player is already standing in front of it.
        bool canEnter = playerInRange && door != null && door.IsOpen;
        SetPrompt(canEnter);

        if (canEnter && inputActions != null && inputActions.Player.Interact.WasPerformedThisFrame())
        {
            Enter();
        }
    }

    private void Enter()
    {
        hasEntered = true;
        SetPrompt(false);

        if (sfxPlayer != null) sfxPlayer.Play(enterSound);

        SceneFader.FadeToScene(nextSceneName, entryPointId);
    }

    private void SetPrompt(bool show)
    {
        if (show == isPromptShown || dialogueUI == null) return;

        isPromptShown = show;

        if (show)
        {
            dialogueUI.Show(promptMessage);
        }
        else
        {
            dialogueUI.Hide();
        }
    }
}
