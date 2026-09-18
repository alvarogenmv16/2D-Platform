using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHealthUI : MonoBehaviour
{
    // =========================
    // VARIABLES
    // =========================

    [Header("Player")]
    [SerializeField] private PlayerHealth playerHealth;

    [Header("Mask UI")]
    // One Image slot per mask icon shown on screen, left to right.
    // The number of masks in the UI is determined by the length of
    // this array, not by maxHealth in PlayerHealth.
    [SerializeField] private Image[] maskSlots;

    [Header("Mask Sprites")]
    [SerializeField] private Sprite fullMask;
    [SerializeField] private Sprite brokenMask;

    [Header("Break Animation")]
    [SerializeField] private float punchDuration = 0.2f;
    [SerializeField] private float punchScale = 0.4f; // how much bigger at the peak of the punch

    // Tracks how many masks were full on the PREVIOUS update, so we can
    // detect exactly which ones just broke. -1 means "not initialized yet",
    // used to skip animating on the very first call (game start).
    private int previousRemainingMasks = -1;

    // =========================
    // START
    // =========================

    private void OnEnable()
    {
        // Subscribe in OnEnable/unsubscribe in OnDisable rather than
        // Start, so re-enabling this UI (e.g. reopening the HUD after
        // a pause menu) doesn't create duplicate listeners.
        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged.AddListener(UpdateMasks);
        }
    }

    private void OnDisable()
    {
        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged.RemoveListener(UpdateMasks);
        }
    }

    // =========================
    // FUNCTIONS
    // =========================

    // Points this UI at a different PlayerHealth after the fact — needed
    // after a mid-fight scene transition, where the surviving Player isn't
    // part of the new scene's saved data and can't be wired via Inspector.
    public void Bind(PlayerHealth health)
    {
        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged.RemoveListener(UpdateMasks);
        }

        playerHealth = health;
        // Forget the previous fight's mask state so the first update after
        // binding just paints the real health instead of animating a break.
        previousRemainingMasks = -1;

        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged.AddListener(UpdateMasks);
            playerHealth.RepublishHealth();
        }
    }

    private void UpdateMasks(float currentHealth, float maxHealth)
    {
        // Normalize health against how many mask icons we actually have
        // on screen, instead of assuming 1 health point = 1 mask.
        // Without this, maxHealth = 100 would require 100 mask slots.
        int totalMasks = maskSlots.Length;
        float healthPercentage = currentHealth / maxHealth;
        int remainingMasks = Mathf.CeilToInt(healthPercentage * totalMasks);

        // First call (game start): just record the initial state,
        // don't play a break animation for masks that were never "lost".
        if (previousRemainingMasks == -1)
        {
            previousRemainingMasks = remainingMasks;
        }
        // Only play the break animation for masks that just transitioned
        // from full to broken in THIS update (not ones already broken).
        else if (remainingMasks < previousRemainingMasks)
        {
            for (int i = remainingMasks; i < previousRemainingMasks; i++)
            {
                if (maskSlots[i] != null)
                {
                    StartCoroutine(PunchScale(maskSlots[i].rectTransform));
                }
            }
        }

        previousRemainingMasks = remainingMasks;

        // Walk every mask slot and decide whether it should show as
        // full or broken based on how many "remaining masks" we have.
        for (int i = 0; i < maskSlots.Length; i++)
        {
            if (maskSlots[i] == null)
            {
                // Warn instead of silently skipping, so a missing
                // reference in the Inspector doesn't go unnoticed.
                Debug.LogWarning($"PlayerHealthUI: mask slot at index {i} is not assigned.");
                continue;
            }

            maskSlots[i].sprite = (i < remainingMasks) ? fullMask : brokenMask;
        }
    }

    // Quickly scales the icon up and back down to its original size,
    // giving visual "punch" to the moment a mask breaks.
    private IEnumerator PunchScale(RectTransform target)
    {
        Vector3 originalScale = target.localScale;
        float elapsed = 0f;

        while (elapsed < punchDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / punchDuration;

            // Sin curve: starts at 1, peaks at the midpoint, returns to 1.
            // Gives a smooth "pop" instead of a linear scale change.
            float scaleMultiplier = 1f + Mathf.Sin(t * Mathf.PI) * punchScale;
            target.localScale = originalScale * scaleMultiplier;

            yield return null;
        }

        target.localScale = originalScale;
    }
}