using UnityEngine;
using UnityEngine.SceneManagement;

// Generic scene-navigation button handler, reused by every menu (Main Menu,
// Settings) — they all just need "load this scene" or "quit". The target
// scene name is set per-button as the OnClick's String argument in the
// Inspector, so one component covers New Game, Continue, Settings, and Back.
public class SceneLoader : MonoBehaviour
{
    // Wired to a button's OnClick, passing the target scene name as its
    // string argument (e.g. "SampleScene", "MainMenu", "SettingsScene").
    public void LoadScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
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
