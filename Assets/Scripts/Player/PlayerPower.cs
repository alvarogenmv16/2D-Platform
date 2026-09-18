using UnityEngine;
using UnityEngine.Events;

public class PlayerPower : MonoBehaviour
{
    // =========================
    // VARIABLES
    // =========================

    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private PlayerMovement playerMovement;

    // How many landed hits it takes to fill the bar from empty to full.
    // Each confirmed hit adds 1 unit of power, and maxPower is derived
    // from this so the bar always fills after exactly this many hits.
    [SerializeField] private int hitsToFillBar = 6;

    private float maxPower;
    private float currentPower;

    // Fired whenever power changes, passing (currentPower, maxPower).
    // The UI power bar subscribes to this to update its fill amount,
    // without PlayerPower needing to know the UI exists at all.
    public UnityEvent<float, float> OnPowerChanged;

    // =========================
    // START
    // =========================

    private void Start()
    {
        maxPower = hitsToFillBar;
        currentPower = 0f;

        // Let any listener (like the UI) initialize with the starting value
        OnPowerChanged?.Invoke(currentPower, maxPower);
    }

    private void Update()
    {
        // Reuse PlayerMovement's InputSystem_Actions instance instead of
        // creating a second one, same convention as PlayerAttack.
        if (playerMovement != null && playerMovement.InputActions.Player.Heal.WasPressedThisFrame())
        {
            TryHeal();
        }
    }

    // =========================
    // FUNCTIONS
    // =========================

    // Re-fires OnPowerChanged with the current values, unprompted by any
    // actual change. Used when a power UI binds to this PlayerPower after
    // the fact, mirroring PlayerHealth.RepublishHealth.
    public void RepublishPower()
    {
        OnPowerChanged?.Invoke(currentPower, maxPower);
    }

    // Adds power, clamped to maxPower. Called once per enemy hit that
    // actually connects, from PlayerAttackHitbox.
    public void AddPower(float amount)
    {
        currentPower = Mathf.Clamp(currentPower + amount, 0f, maxPower);
        OnPowerChanged?.Invoke(currentPower, maxPower);
    }

    // Spends a full bar to fully heal the player. Returns false without
    // doing anything if the bar isn't full yet, so the heal button can't
    // be used for a partial heal.
    public bool TryHeal()
    {
        if (currentPower < maxPower) return false;

        playerHealth.FullHeal();
        currentPower = 0f;
        OnPowerChanged?.Invoke(currentPower, maxPower);

        return true;
    }
}
