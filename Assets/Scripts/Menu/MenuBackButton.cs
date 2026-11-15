using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Goes on a menu's "Back" button: Esc (keyboard) or B / Circle (gamepad's
// east button) presses it, the same as clicking it - so a menu screen like
// Settings can be left without the mouse.
[RequireComponent(typeof(Button))]
public class MenuBackButton : MonoBehaviour
{
    private Button button;
    private InputAction backAction;

    private void Awake()
    {
        button = GetComponent<Button>();

        backAction = new InputAction("Back", InputActionType.Button);
        backAction.AddBinding("<Keyboard>/escape");
        backAction.AddBinding("<Gamepad>/buttonEast");
    }

    private void OnEnable() => backAction.Enable();
    private void OnDisable() => backAction.Disable();
    private void OnDestroy() => backAction.Dispose();

    private void Update()
    {
        if (backAction.WasPressedThisFrame() && button.IsInteractable())
        {
            button.onClick.Invoke();
        }
    }
}
