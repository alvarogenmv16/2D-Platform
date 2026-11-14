using TMPro;
using UnityEngine;

// Generic on-screen dialogue box: shows/hides a panel with one line of text
// at a time. Deliberately knows nothing about WHO is talking - any
// interactable (the companion now, others later) can drive the same
// instance, so there's only ever one dialogue box in a scene.
public class DialogueUI : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private TMP_Text lineText;

    public bool IsOpen => panel != null && panel.activeSelf;

    // Whoever showed the current line, when they said (optional). Lets a
    // speaker close the box only if it's still showing *their* line - e.g. a
    // tutorial hint the player walks out of must not close a conversation
    // that another one (or a companion) has opened since.
    private Object owner;

    private void Awake()
    {
        panel.SetActive(false);
    }

    public void Show(string line, Object speaker = null)
    {
        if (panel == null) return;

        owner = speaker;
        panel.SetActive(true);
        lineText.text = line;
    }

    // speaker null = close whatever is showing (the original behaviour).
    public void Hide(Object speaker = null)
    {
        // panel can already be destroyed here if this fires while the scene
        // is tearing down (e.g. stopping Play mode) - OnTriggerExit2D can
        // still run mid-teardown, after panel's GameObject is gone.
        if (panel == null) return;
        if (speaker != null && speaker != owner) return;

        owner = null;
        panel.SetActive(false);
    }

    public bool IsShowing(Object speaker) => IsOpen && speaker != null && owner == speaker;
}
