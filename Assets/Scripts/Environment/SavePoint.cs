using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;

// A lamp post the player can save at (Prefabs/SavePoint1/2/3, one look per
// world). Standing by it shows a prompt in the scene's DialogueUI - the same
// box companions and doors use - and Interact (E / gamepad north) saves the
// game (SaveSystem), fully heals the player and flares the lantern up.
//
// "Continue" in the main menu brings the player back here: the save stores
// this scene and this point's id, so every SavePoint in a scene needs its
// own id.
//
// Hierarchy:
//   SavePoint (root) - BoxCollider2D (trigger: the zone), SavePoint, AudioSource
//     Visuals        - SpriteRenderer (the lamp post, pivot at its base)
//     Lantern        - empty, at the lantern: where the glow sits
[RequireComponent(typeof(BoxCollider2D))]
public class SavePoint : MonoBehaviour
{
    [SerializeField] private string savePointId = "Save";
    // Where the player appears on Continue, relative to the lamp's base.
    [SerializeField] private Vector2 spawnOffset = new Vector2(1.2f, 0.1f);

    [Header("Prompt")]
    [SerializeField] private string promptMessage = "Guardar partida";
    [SerializeField] private string savedMessage = "Partida guardada";
    [SerializeField] private float savedMessageTime = 1.5f;
    [SerializeField] private bool healOnSave = true;
    [SerializeField] private DialogueUI dialogueUI; // found in the scene if empty

    [Header("Lantern glow")]
    [SerializeField] private Transform lantern;
    [SerializeField] private Color glowColor = new Color(1f, 0.35f, 0.5f);
    [SerializeField] private float idleIntensity = 0.6f;
    [SerializeField] private float savedIntensity = 3f;
    [SerializeField] private float glowRadius = 3f;
    [SerializeField] private float flickerAmount = 0.12f;

    [Header("Sound")]
    [SerializeField] private AudioClip saveSound;
    [SerializeField, Range(0f, 1f)] private float saveVolume = 1f;

    public string Id => savePointId;
    public Vector3 SpawnPosition => transform.position + (Vector3)spawnOffset;

    private InputSystem_Actions inputActions;
    private GameObject player;
    private bool playerInRange;
    private bool busy;
    private Light2D glow;
    private float glowBoost; // extra intensity right after saving, fades out

    public static SavePoint Find(string id)
    {
        foreach (SavePoint point in FindObjectsByType<SavePoint>(FindObjectsSortMode.None))
        {
            if (point.savePointId == id) return point;
        }
        return null;
    }

    private void Awake()
    {
        GetComponent<BoxCollider2D>().isTrigger = true;

        Transform glowAt = lantern != null ? lantern : transform;
        glow = glowAt.gameObject.AddComponent<Light2D>();
        glow.lightType = Light2D.LightType.Point;
        glow.color = glowColor;
        glow.pointLightInnerRadius = 0f;
        glow.pointLightOuterRadius = glowRadius;
        glow.intensity = idleIntensity;
    }

    private void Start()
    {
        if (dialogueUI == null) dialogueUI = FindFirstObjectByType<DialogueUI>(FindObjectsInactive.Include);
    }

    private void Update()
    {
        float flicker = 1f + (Mathf.PerlinNoise(Time.time * 3f, transform.position.x) - 0.5f) * 2f * flickerAmount;
        glowBoost = Mathf.MoveTowards(glowBoost, 0f, Time.deltaTime * (savedIntensity - idleIntensity));
        glow.intensity = (idleIntensity + glowBoost) * flicker;

        if (!playerInRange || busy || inputActions == null) return;

        if (inputActions.Player.Interact.WasPerformedThisFrame())
        {
            StartCoroutine(SaveSequence());
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        playerInRange = true;
        player = other.gameObject;
        if (other.TryGetComponent(out PlayerMovement movement)) inputActions = movement.InputActions;

        if (!busy) ShowPrompt();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        playerInRange = false;
        if (dialogueUI != null) dialogueUI.Hide(this);
    }

    private void ShowPrompt()
    {
        if (dialogueUI != null) dialogueUI.Show($"{promptMessage}  <b>[{InteractKey()}]</b>", this);
    }

    private string InteractKey()
    {
        if (inputActions == null) return "E";
        string shown = inputActions.Player.Interact.GetBindingDisplayString(InputBinding.MaskByGroup("Keyboard&Mouse"));
        return string.IsNullOrEmpty(shown) ? "E" : shown;
    }

    private IEnumerator SaveSequence()
    {
        busy = true;

        if (healOnSave && player != null && player.TryGetComponent(out PlayerHealth health))
        {
            health.FullHeal();
        }

        SaveSystem.Save(savePointId, player);

        glowBoost = savedIntensity - idleIntensity;
        if (saveSound != null && TryGetComponent(out AudioSource source)) source.PlayOneShot(saveSound, saveVolume);
        if (dialogueUI != null) dialogueUI.Show(savedMessage, this);

        yield return new WaitForSeconds(savedMessageTime);

        busy = false;
        if (playerInRange) ShowPrompt();
        else if (dialogueUI != null) dialogueUI.Hide(this);
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(SpawnPosition, 0.3f);
        UnityEditor.Handles.Label(transform.position + Vector3.up * 0.3f, "Save: " + savePointId);
    }
#endif
}
