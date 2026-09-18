using UnityEngine;
using UnityEngine.UI;

public class PlayerPowerUI : MonoBehaviour
{
    // =========================
    // VARIABLES
    // =========================

    [Header("Player")]
    [SerializeField] private PlayerPower playerPower;

    [Header("Bar UI")]
    // Image.Type must be set to Filled (Horizontal, Origin: Left) in the
    // Inspector, otherwise fillAmount has no visible effect.
    [SerializeField] private Image fillImage;

    // =========================
    // START
    // =========================

    private void OnEnable()
    {
        // Subscribe in OnEnable/unsubscribe in OnDisable rather than
        // Start, matching PlayerHealthUI, so re-enabling this UI doesn't
        // create duplicate listeners.
        if (playerPower != null)
        {
            playerPower.OnPowerChanged.AddListener(UpdateBar);
        }
    }

    private void OnDisable()
    {
        if (playerPower != null)
        {
            playerPower.OnPowerChanged.RemoveListener(UpdateBar);
        }
    }

    // =========================
    // FUNCTIONS
    // =========================

    // Points this UI at a different PlayerPower after the fact, following
    // the same rebind pattern as PlayerHealthUI.Bind.
    public void Bind(PlayerPower power)
    {
        if (playerPower != null)
        {
            playerPower.OnPowerChanged.RemoveListener(UpdateBar);
        }

        playerPower = power;

        if (playerPower != null)
        {
            playerPower.OnPowerChanged.AddListener(UpdateBar);
            playerPower.RepublishPower();
        }
    }

    private void UpdateBar(float currentPower, float maxPower)
    {
        if (fillImage == null)
        {
            Debug.LogWarning("PlayerPowerUI: fillImage is not assigned.");
            return;
        }

        fillImage.fillAmount = maxPower > 0f ? currentPower / maxPower : 0f;
    }
}
