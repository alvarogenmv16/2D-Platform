using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// The names shown on screen for the controls ("Press [X] to jump"), always
// in English, and for the device the player is actually using: keyboard &
// mouse, or a gamepad named the PlayStation way (X, Circle, Square,
// Triangle, L1, R1...) instead of the Input System's generic "Button South".
//
// Used by TutorialHint ({Jump}, {Attack}... in its messages) and SavePoint's
// prompt. Swaps between keyboard and gamepad by itself, depending on what
// the player last touched (UsingGamepad).
public static class ControlNames
{
    // What the player is currently playing with - shared by every prompt.
    public static bool UsingGamepad { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => UsingGamepad = false;

    // Watches the devices every frame - one hidden object for the whole game.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateWatcher()
    {
        GameObject watcher = new GameObject("ControlNamesWatcher");
        watcher.hideFlags = HideFlags.HideInHierarchy;
        Object.DontDestroyOnLoad(watcher);
        watcher.AddComponent<DeviceWatcher>();
    }

    private class DeviceWatcher : MonoBehaviour
    {
        private void Update()
        {
            Gamepad pad = Gamepad.current;
            if (!UsingGamepad)
            {
                if (pad != null && (pad.leftStick.ReadValue().sqrMagnitude > 0.25f
                                    || pad.rightStick.ReadValue().sqrMagnitude > 0.25f || AnyButton(pad)))
                {
                    UsingGamepad = true;
                }
            }
            else if ((Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame)
                     || (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame))
            {
                UsingGamepad = false;
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
    }

    // Gamepad controls, PlayStation names.
    private static readonly Dictionary<string, string> GamepadNames = new Dictionary<string, string>
    {
        { "<Gamepad>/buttonSouth", "X" },
        { "<Gamepad>/buttonEast", "Circle" },
        { "<Gamepad>/buttonWest", "Square" },
        { "<Gamepad>/buttonNorth", "Triangle" },
        { "<Gamepad>/leftShoulder", "L1" },
        { "<Gamepad>/rightShoulder", "R1" },
        { "<Gamepad>/leftTrigger", "L2" },
        { "<Gamepad>/rightTrigger", "R2" },
        { "<Gamepad>/leftStickPress", "L3" },
        { "<Gamepad>/rightStickPress", "R3" },
        { "<Gamepad>/start", "Options" },
        { "<Gamepad>/select", "Share" },
        { "<Gamepad>/leftStick", "Left Stick" },
        { "<Gamepad>/rightStick", "Right Stick" },
        { "<Gamepad>/dpad", "D-Pad" },
        { "<Gamepad>/dpad/up", "D-Pad Up" },
        { "<Gamepad>/dpad/down", "D-Pad Down" },
        { "<Gamepad>/dpad/left", "D-Pad Left" },
        { "<Gamepad>/dpad/right", "D-Pad Right" },
    };

    private static readonly Dictionary<string, string> KeyboardMouseNames = new Dictionary<string, string>
    {
        { "<Mouse>/leftButton", "Left Click" },
        { "<Mouse>/rightButton", "Right Click" },
        { "<Mouse>/middleButton", "Middle Click" },
    };

    // An action of the Player map ("Jump", "Attack"...) -> its button(s) on
    // the current device, e.g. "Space" / "X", "Left Click / Enter" / "Square".
    // A few names that aren't actions are also understood: Move, Up, Down.
    // Anything unknown comes back as it is.
    public static string For(InputSystem_Actions actions, string actionName)
    {
        return For(actions, actionName, UsingGamepad);
    }

    public static string For(InputSystem_Actions actions, string actionName, bool gamepad)
    {
        // Directions are composites (WASD + arrows) or a stick: spelled out.
        switch (actionName)
        {
            case "Move": return gamepad ? "Left Stick" : "A / D";
            case "Up": return gamepad ? "Left Stick Up" : "W / Up Arrow";
            // Looking down (PlayerCameraLook): the right stick on a gamepad.
            case "Down": return gamepad ? "Right Stick Down" : "S / Down Arrow";
        }

        if (actions == null) return actionName;
        InputAction action = actions.asset.FindAction("Player/" + actionName);
        if (action == null) return actionName;

        // Names come from the binding paths, not from the connected device:
        // GetBindingDisplayString asks the device, and Windows names keys in
        // the OS language ("Espacio" for Space), gamepads come out as
        // "Button South"...
        InputBinding mask = InputBinding.MaskByGroup(gamepad ? "Gamepad" : "Keyboard&Mouse");
        List<string> names = new List<string>();
        foreach (InputBinding binding in action.bindings)
        {
            if (binding.isComposite || binding.isPartOfComposite) continue;
            if (!mask.Matches(binding)) continue;

            string path = binding.effectivePath;
            if (!GamepadNames.TryGetValue(path, out string name) && !KeyboardMouseNames.TryGetValue(path, out name))
            {
                name = InputControlPath.ToHumanReadableString(path, InputControlPath.HumanReadableStringOptions.OmitDevice);
            }
            if (!string.IsNullOrEmpty(name) && !names.Contains(name)) names.Add(name);
        }

        return names.Count > 0 ? string.Join(" / ", names) : actionName;
    }
}
