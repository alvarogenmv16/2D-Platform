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

    private void UpdateMasks(float currentHealth, float maxHealth)
    {
        // Normalize health against how many mask icons we actually have
        // on screen, instead of assuming 1 health point = 1 mask.
        // Without this, maxHealth = 5 would require 5 mask slots.
        int totalMasks = maskSlots.Length;
        float healthPercentage = currentHealth / maxHealth;
        int remainingMasks = Mathf.CeilToInt(healthPercentage * totalMasks);

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
}