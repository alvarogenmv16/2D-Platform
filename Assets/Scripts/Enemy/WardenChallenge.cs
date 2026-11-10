using System.Collections;
using UnityEngine;
using UnityEngine.Events;

// TempleScene: the spot where the true final boss (the Warden) is challenged.
// A spear stuck in the ground (WardenSpear0) that only shows up once Morthos
// is dead. Standing on it shows the "Desafiar" prompt in the scene's
// DialogueUI; pressing Interact accepts the challenge and fires
// OnChallengeAccepted (the Warden fight hooks onto it). If the player never
// presses it, nothing happens.
//
// The Beast and the DialogueUI are found in the scene if left empty (this is
// a prefab, it can't reference scene objects).
[RequireComponent(typeof(Collider2D))]
public class WardenChallenge : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spearRenderer;

    [Header("Appear after Morthos")]
    // Off = the spot is there from the start (handy to test without the Beast).
    [SerializeField] private bool appearAfterBeast = true;
    [SerializeField] private BeastAI beast;
    // Lets the death animation and the victory music breathe first.
    [SerializeField] private float appearDelay = 3f;
    [SerializeField] private float fadeInTime = 1.5f;

    [Header("Prompt")]
    [SerializeField] private DialogueUI dialogueUI;
    [SerializeField] private string promptMessage = "Desafiar";

    [Header("Sound (optional)")]
    [SerializeField] private SfxPlayer sfxPlayer;
    [SerializeField] private AudioClip appearSound;
    [SerializeField] private AudioClip challengeSound;

    public UnityEvent OnChallengeAccepted;

    public bool IsAvailable => isAvailable;

    private Collider2D triggerZone;
    private EnemyHealth beastHealth;
    private InputSystem_Actions inputActions;
    private bool playerInRange;
    private bool isAvailable;
    private bool isPromptShown;
    private bool hasChallenged;

    private void Awake()
    {
        triggerZone = GetComponent<Collider2D>();
        triggerZone.isTrigger = true;

        if (spearRenderer == null) spearRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    private void Start()
    {
        if (dialogueUI == null)
        {
            dialogueUI = FindFirstObjectByType<DialogueUI>(FindObjectsInactive.Include);
        }

        if (beast == null) beast = FindFirstObjectByType<BeastAI>(FindObjectsInactive.Include);
        if (beast != null) beastHealth = beast.GetComponent<EnemyHealth>();

        if (appearAfterBeast && beastHealth != null)
        {
            SetVisible(false);
            beastHealth.OnDied.AddListener(HandleBeastDied);
        }
        else
        {
            // No Beast (already gone, or testing): available from the start.
            SetVisible(true);
        }
    }

    private void OnDestroy()
    {
        if (beastHealth != null) beastHealth.OnDied.RemoveListener(HandleBeastDied);
    }

    private void HandleBeastDied()
    {
        StartCoroutine(Appear());
    }

    private IEnumerator Appear()
    {
        yield return new WaitForSeconds(appearDelay);

        if (sfxPlayer != null) sfxPlayer.Play(appearSound);

        if (spearRenderer != null)
        {
            spearRenderer.enabled = true;
            Color c = spearRenderer.color;
            for (float t = 0f; t < fadeInTime; t += Time.deltaTime)
            {
                c.a = t / fadeInTime;
                spearRenderer.color = c;
                yield return null;
            }
            c.a = 1f;
            spearRenderer.color = c;
        }

        isAvailable = true;
        triggerZone.enabled = true;
    }

    private void SetVisible(bool visible)
    {
        isAvailable = visible;
        triggerZone.enabled = visible;

        if (spearRenderer != null)
        {
            spearRenderer.enabled = visible;
            Color c = spearRenderer.color;
            c.a = visible ? 1f : 0f;
            spearRenderer.color = c;
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
    }

    private void Update()
    {
        if (hasChallenged) return;

        bool canChallenge = isAvailable && playerInRange;
        SetPrompt(canChallenge);

        if (canChallenge && inputActions != null && inputActions.Player.Interact.WasPerformedThisFrame())
        {
            Challenge();
        }
    }

    private void Challenge()
    {
        hasChallenged = true;
        SetPrompt(false);

        if (sfxPlayer != null) sfxPlayer.Play(challengeSound);

        if (OnChallengeAccepted != null) OnChallengeAccepted.Invoke();
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
