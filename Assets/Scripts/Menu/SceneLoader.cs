using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Generic scene-navigation button handler, reused by every menu (Main Menu,
// Settings). Buttons wire their OnClick to one of these methods; the ones
// taking a string get the target scene name as the OnClick's String
// argument in the Inspector.
public class SceneLoader : MonoBehaviour
{
    // Main menu only: greyed out while there's no save file to continue from.
    [SerializeField] private Button continueButton;

    private void Start()
    {
        if (continueButton != null) continueButton.interactable = SaveSystem.HasSave;
    }

    // Plain scene change (Settings, Back...).
    public void LoadScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }

    // "New Game": forgets the current run's progress and starts at
    // sceneName (the tutorial). The save file is only replaced once the
    // player saves again.
    public void NewGame(string sceneName)
    {
        SaveSystem.NewGame(sceneName);
    }

    // "Continue": back to the last save point, with the saved progress.
    public void ContinueGame()
    {
        if (!SaveSystem.Continue() && continueButton != null)
        {
            continueButton.interactable = false;
        }
    }

    // Wired to a Quit button's OnClick. Application.Quit() is a no-op inside
    // the Unity Editor, so this also stops Play Mode there — lets the button
    // be verified without needing a real build every time.
    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
