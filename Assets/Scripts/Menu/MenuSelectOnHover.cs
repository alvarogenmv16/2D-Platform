using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Added by MenuNavigator to each of its buttons: hovering one with the mouse
// selects it (Unity only highlights it by default), so mouse and
// keyboard/gamepad always agree on which button is the current one.
public class MenuSelectOnHover : MonoBehaviour, IPointerEnterHandler
{
    public void OnPointerEnter(PointerEventData eventData)
    {
        Selectable s = GetComponent<Selectable>();
        if (s != null && s.IsInteractable() && EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(gameObject);
        }
    }
}
