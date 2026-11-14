using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.InputSystem;

// A tutorial message tied to a spot of the level. Its BoxCollider2D (trigger)
// is the zone: while the player is inside, the message is shown in the
// scene's dialogue box - the same DialogueUI companions talk through, so
// the game reads the same everywhere - and it closes when they walk out
// (or never shows again, with showOnce).
//
// The message can name controls with {Action} - {Move}, {Jump}, {Attack},
// {Dash}, {Heal}, {Interact}... (any action of the Player map). They're
// replaced by the actual key or button, and swap between keyboard and
// gamepad on their own depending on what the player last touched.
//   e.g. "Pulsa {Jump} para saltar"  ->  "Pulsa [Space] para saltar"
//
// In the Editor the zone and the message are drawn in the Scene view, so
// hints can be placed and read without entering Play mode.
[RequireComponent(typeof(BoxCollider2D))]
public class TutorialHint : MonoBehaviour
{
    [SerializeField, TextArea(2, 4)] private string message = "Press {Jump} to jump";
    [SerializeField] private Color keyColor = new Color(0.8f, 0.5f, 1f);
    // On = once it has been shown and the player leaves, it never comes back.
    [SerializeField] private bool showOnce = false;
    [SerializeField] private DialogueUI dialogueUI; // found in the scene if empty

    // Shared by every hint: what the player is currently playing with.
    private static bool usingGamepad;

    private InputSystem_Actions inputActions;
    private bool playerInside;
    private bool shownOnce;
    private bool shownWithGamepad;

    private static readonly Regex Placeholder = new Regex(@"\{(\w+)\}");

    private void Start()
    {
        GetComponent<BoxCollider2D>().isTrigger = true;

        if (dialogueUI == null)
        {
            dialogueUI = FindFirstObjectByType<DialogueUI>(FindObjectsInactive.Include);
            if (dialogueUI == null)
            {
                Debug.LogWarning($"{name}: TutorialHint found no DialogueUI in the scene.", this);
            }
        }
    }

    private void Update()
    {
        DetectDevice();

        // Switched between keyboard and gamepad while reading: re-show with
        // the right keys - but only if the box is still showing this hint.
        if (playerInside && shownWithGamepad != usingGamepad && dialogueUI != null && dialogueUI.IsShowing(this))
        {
            Show();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (showOnce && shownOnce) return;

        playerInside = true;
        if (other.TryGetComponent(out PlayerMovement movement))
        {
            inputActions = movement.InputActions;
        }

        Show();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player") || !playerInside) return;

        playerInside = false;
        shownOnce = true;
        if (dialogueUI != null) dialogueUI.Hide(this);
    }

    private void Show()
    {
        if (dialogueUI == null) return;

        shownWithGamepad = usingGamepad;
        string hex = ColorUtility.ToHtmlStringRGB(keyColor);
        string text = Placeholder.Replace(message, m => $"<color=#{hex}><b>[{ControlName(m.Groups[1].Value)}]</b></color>");
        dialogueUI.Show(text, this);
    }

    private string ControlName(string actionName)
    {
        // Movement is a composite (WASD + arrows): spell it out instead of
        // listing every key.
        if (actionName == "Move") return usingGamepad ? "Left Stick" : "A / D";

        if (inputActions == null) return actionName;
        InputAction action = inputActions.asset.FindAction("Player/" + actionName);
        if (action == null) return actionName;

        // Names come from the binding paths (the control layouts), not from
        // the connected keyboard: GetBindingDisplayString asks the device,
        // and Windows names keys in the OS language ("Espacio" for Space).
        // The layout names are always the English ones.
        string group = usingGamepad ? "Gamepad" : "Keyboard&Mouse";
        InputBinding mask = InputBinding.MaskByGroup(group);
        List<string> names = new List<string>();
        foreach (InputBinding binding in action.bindings)
        {
            if (binding.isComposite || binding.isPartOfComposite) continue;
            if (!mask.Matches(binding)) continue;

            string path = binding.effectivePath;
            string name = path switch
            {
                "<Mouse>/leftButton" => "Left Click",
                "<Mouse>/rightButton" => "Right Click",
                "<Mouse>/middleButton" => "Middle Click",
                _ => InputControlPath.ToHumanReadableString(path, InputControlPath.HumanReadableStringOptions.OmitDevice),
            };
            if (!string.IsNullOrEmpty(name) && !names.Contains(name)) names.Add(name);
        }

        return names.Count > 0 ? string.Join(" / ", names) : actionName;
    }

    // =========================
    // DEVICE
    // =========================

    private static void DetectDevice()
    {
        Gamepad pad = Gamepad.current;
        if (!usingGamepad)
        {
            if (pad != null && (pad.leftStick.ReadValue().sqrMagnitude > 0.25f || AnyButton(pad)))
            {
                usingGamepad = true;
            }
        }
        else if ((Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame)
                 || (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame))
        {
            usingGamepad = false;
        }
    }

    private static bool AnyButton(Gamepad pad)
    {
        foreach (InputControl control in pad.allControls)
        {
            if (control is UnityEngine.InputSystem.Controls.ButtonControl button && button.wasPressedThisFrame) return true;
        }
        return false;
    }

    // =========================
    // EDITOR PREVIEW
    // =========================
#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        BoxCollider2D box = GetComponent<BoxCollider2D>();
        if (box == null) return;

        Vector2 center = (Vector2)transform.position + Vector2.Scale(box.offset, transform.lossyScale);
        Vector2 size = Vector2.Scale(box.size, transform.lossyScale);
        Gizmos.color = new Color(0.7f, 0.4f, 1f, 0.6f);
        Gizmos.DrawWireCube(center, size);

        GUIStyle style = new GUIStyle(UnityEditor.EditorStyles.boldLabel) { alignment = TextAnchor.MiddleCenter };
        style.normal.textColor = new Color(0.85f, 0.7f, 1f);
        UnityEditor.Handles.Label(center + Vector2.up * (size.y * 0.5f + 0.5f), message, style);
    }
#endif
}
