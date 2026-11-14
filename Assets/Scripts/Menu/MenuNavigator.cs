using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Keyboard/gamepad navigation and a "move" sound for a menu (main menu,
// pause menu). Goes on the object that holds the menu's buttons:
//   - On enable it selects the first usable button of firstSelected (in
//     order: e.g. Continue, else New Game), so arrows / stick work at once.
//   - Hovering a button with the mouse selects it too, so mouse and
//     keyboard/gamepad always agree on which one is highlighted.
//   - If the selection is lost (clicking empty space), the last button is
//     selected again, so the gamepad never gets stuck with nothing selected.
//   - Every time the selected button changes it plays moveSound. Without a
//     clip assigned it plays a short generated tick.
public class MenuNavigator : MonoBehaviour
{
    [SerializeField] private Selectable[] firstSelected; // the first usable one wins; empty = first button found

    [Header("Sound")]
    [SerializeField] private AudioClip moveSound; // empty = generated tick
    [SerializeField, Range(0f, 1f)] private float moveVolume = 0.5f;

    private AudioSource source;
    private GameObject lastSelected;
    private static AudioClip generatedTick;

    // Anything quieter than this at the start of moveSound is cut off, so
    // the sound starts the instant the selection moves (many mp3s carry a
    // few dozen ms of silence/encoder padding before the actual sound).
    [SerializeField, Range(0f, 0.2f)] private float silenceThreshold = 0.02f;
    private static readonly Dictionary<AudioClip, AudioClip> trimmedClips = new Dictionary<AudioClip, AudioClip>();

    // Used by PauseMenu, which builds its buttons in code.
    public void Setup(Selectable[] first, AudioClip sound, float volume)
    {
        firstSelected = first;
        if (sound != null) moveSound = TrimLeadingSilence(sound, silenceThreshold);
        moveVolume = volume;
        AddHoverSelect();
    }

    private void Awake()
    {
        source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 0f;
        // The pause menu sets AudioListener.pause; its own sounds must still play.
        source.ignoreListenerPause = true;

        moveSound = moveSound == null ? GetGeneratedTick() : TrimLeadingSilence(moveSound, silenceThreshold);
        AddHoverSelect();
    }

    private void OnEnable()
    {
        lastSelected = null;
        SelectFirst();
        // Again next frame: other scripts' Start (e.g. SceneLoader greying out
        // Continue) may change which button is usable.
        StartCoroutine(SelectFirstNextFrame());
    }

    private IEnumerator SelectFirstNextFrame()
    {
        yield return null;
        GameObject current = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
        if (!IsUsable(current))
        {
            SelectFirst();
            lastSelected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
        }
    }

    private void Update()
    {
        EventSystem es = EventSystem.current;
        if (es == null) return;

        GameObject current = es.currentSelectedGameObject;

        if (!IsUsable(current))
        {
            // Lost the selection: give it back without a sound.
            if (IsUsable(lastSelected)) es.SetSelectedGameObject(lastSelected);
            else SelectFirst();
            lastSelected = es.currentSelectedGameObject;
            return;
        }

        if (current != lastSelected)
        {
            if (lastSelected != null) PlayMove();
            lastSelected = current;
        }
    }

    private void SelectFirst()
    {
        EventSystem es = EventSystem.current;
        if (es == null) return;

        Selectable target = null;
        if (firstSelected != null)
        {
            foreach (Selectable s in firstSelected)
            {
                if (s != null && s.IsInteractable() && s.isActiveAndEnabled) { target = s; break; }
            }
        }
        if (target == null)
        {
            foreach (Selectable s in GetComponentsInChildren<Selectable>())
            {
                if (s.IsInteractable()) { target = s; break; }
            }
        }

        if (target != null) es.SetSelectedGameObject(target.gameObject);
    }

    // Usable = one of this menu's own buttons, active and interactable.
    private bool IsUsable(GameObject go)
    {
        if (go == null || !go.activeInHierarchy || !go.transform.IsChildOf(transform)) return false;
        Selectable s = go.GetComponent<Selectable>();
        return s != null && s.IsInteractable();
    }

    private void PlayMove()
    {
        if (moveSound != null) source.PlayOneShot(moveSound, moveVolume);
    }

    private void AddHoverSelect()
    {
        foreach (Selectable s in GetComponentsInChildren<Selectable>(true))
        {
            if (!s.TryGetComponent(out MenuSelectOnHover _)) s.gameObject.AddComponent<MenuSelectOnHover>();
        }
    }

    // A copy of clip starting at its first audible sample (minus ~2 ms, so
    // the attack isn't clipped). Cached per clip; the original asset is left
    // untouched. Needs the clip's Load Type to be Decompress On Load (the
    // default) - otherwise the original is returned as it is.
    private static AudioClip TrimLeadingSilence(AudioClip clip, float threshold)
    {
        if (clip == null) return null;
        if (trimmedClips.TryGetValue(clip, out AudioClip cached)) return cached;

        AudioClip result = clip;
        if (clip.loadState != AudioDataLoadState.Loaded) clip.LoadAudioData();

        int channels = clip.channels;
        float[] data = new float[clip.samples * channels];
        if (clip.loadState == AudioDataLoadState.Loaded && clip.GetData(data, 0))
        {
            int first = 0;
            while (first < data.Length && Mathf.Abs(data[first]) < threshold) first++;

            int startFrame = Mathf.Max(0, first / channels - clip.frequency / 500); // back off ~2 ms
            if (first < data.Length && startFrame > 0)
            {
                int frames = clip.samples - startFrame;
                float[] trimmed = new float[frames * channels];
                System.Array.Copy(data, startFrame * channels, trimmed, 0, trimmed.Length);

                result = AudioClip.Create(clip.name + "_trimmed", frames, channels, clip.frequency, false);
                result.SetData(trimmed, 0);
            }
        }

        trimmedClips[clip] = result;
        return result;
    }

    // A short soft "tick": a falling sine blip, ~45 ms.
    private static AudioClip GetGeneratedTick()
    {
        if (generatedTick != null) return generatedTick;

        const int rate = 44100;
        int samples = rate * 45 / 1000;
        float[] data = new float[samples];
        float phase = 0f;
        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / samples;
            float freq = Mathf.Lerp(1400f, 900f, t);
            phase += 2f * Mathf.PI * freq / rate;
            float envelope = Mathf.Min(1f, i / (rate * 0.002f)) * Mathf.Pow(1f - t, 3f);
            data[i] = Mathf.Sin(phase) * envelope * 0.6f;
        }

        generatedTick = AudioClip.Create("MenuTick", samples, 1, rate, false);
        generatedTick.SetData(data, 0);
        return generatedTick;
    }
}
