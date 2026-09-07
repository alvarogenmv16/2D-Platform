using System.Collections;
using UnityEngine;

// Sits on the floor patch under the Eye's Phase 1 arena. Called by
// EyeAI.Phase2TransitionSequence once health crosses the phase threshold:
// plays a flipbook through the crumble frames, then drops the collider so
// the player (and, later, the Eye itself) fall through into the room below.
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Collider2D))]
public class EyeFloorCrumble : MonoBehaviour
{
    // =========================
    // VARIABLES
    // =========================

    [SerializeField] private Sprite[] crumbleFrames;
    [SerializeField] private float secondsPerFrame = 0.12f;

    [Header("Sound")]
    [SerializeField] private SfxPlayer sfxPlayer;
    [SerializeField] private AudioClip crumbleSound;

    private SpriteRenderer spriteRenderer;
    private Collider2D solidCollider;
    private bool hasCrumbled = false;

    // =========================
    // START
    // =========================
    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        solidCollider = GetComponent<Collider2D>();
    }

    // =========================
    // FUNCTIONS
    // =========================

    public IEnumerator Crumble()
    {
        if (hasCrumbled) yield break;
        hasCrumbled = true;

        sfxPlayer?.Play(crumbleSound);

        foreach (Sprite frame in crumbleFrames)
        {
            spriteRenderer.sprite = frame;
            yield return new WaitForSeconds(secondsPerFrame);
        }

        // Falls last, after the visual is already mid-collapse, so the
        // player drops through roughly when the floor looks broken instead
        // of on the very first cracked frame.
        solidCollider.enabled = false;
    }
}
