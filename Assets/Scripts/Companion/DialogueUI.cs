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

    public bool IsOpen => panel.activeSelf;

    private void Awake()
    {
        panel.SetActive(false);
    }

    public void Show(string line)
    {
        panel.SetActive(true);
        lineText.text = line;
    }

    public void Hide()
    {
        panel.SetActive(false);
    }
}
