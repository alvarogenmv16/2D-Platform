using UnityEngine;
using UnityEngine.Events;

public class PlayerPower : MonoBehaviour
{
    // =========================
    // VARIABLES
    // =========================

    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private Animator animator;

    // How many landed hits it takes to fill the bar from empty to full
    // with the starting damage. Each hit adds a bit more power per damage
    // upgrade (see PlayerAttackHitbox.powerPerExtraDamage), so upgrades
    // fill the bar faster.
    [SerializeField] private int hitsToFillBar = 7;

    private float maxPower;
    private float currentPower;

    // True while the heal animation is playing, between TryHeal() being
    // called and the ApplyHeal animation event landing. Blocks a second
    // heal from starting mid-animation.
    private bool isHealing;

    // If the heal animation is cut short before its ApplyHeal event - a hit,
    // dash, jump or attack (all Any State transitions), death's Rebind, a
    // scene change - the event never comes and isHealing would stay true
    // for good, so the player could never heal again. Update watches for
    // that (see CheckHealInterrupted) once the Animator has had this long
    // to actually enter PlayerHeal.
    private const float HealStartGrace = 0.2f;
    private static readonly int HealState = Animator.StringToHash("PlayerHeal");
    private float healStartedAt;

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
        if (isHealing) CheckHealInterrupted();

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
    // actually connects, from PlayerAttackHitbox (amount = damage relative
    // to the starting damage).
    public void AddPower(float amount)
    {
        currentPower = Mathf.Clamp(currentPower + amount, 0f, maxPower);
        OnPowerChanged?.Invoke(currentPower, maxPower);
    }

    // Spends a full bar and starts the heal animation. Returns false
    // without doing anything if the bar isn't full yet or a heal is
    // already playing, so the heal button can't be used for a partial
    // heal or retriggered mid-animation. The actual health restore
    // happens later, in ApplyHeal, once the animation reaches the
    // moment it should visually land - healing is not instant.
    public bool TryHeal()
    {
        if (isHealing || currentPower < maxPower) return false;

        isHealing = true;
        healStartedAt = Time.time;
        currentPower = 0f;
        OnPowerChanged?.Invoke(currentPower, maxPower);

        animator?.SetTrigger("HealTrigger");

        return true;
    }

    // The heal was cancelled without landing: unlock healing again and give
    // the bar back, since no health was restored for it.
    private void CheckHealInterrupted()
    {
        if (animator == null || Time.time - healStartedAt < HealStartGrace) return;

        bool inHeal = animator.GetCurrentAnimatorStateInfo(0).shortNameHash == HealState
            || (animator.IsInTransition(0) && animator.GetNextAnimatorStateInfo(0).shortNameHash == HealState);
        if (inHeal) return;

        isHealing = false;
        animator.ResetTrigger("HealTrigger");
        currentPower = maxPower;
        OnPowerChanged?.Invoke(currentPower, maxPower);
    }

    // Called via an Animation Event on the PlayerHeal clip, at the exact
    // frame the heal should actually land - not on button press, so the
    // health restore is tied to the animation instead of being instant.
    public void ApplyHeal()
    {
        playerHealth.FullHeal();
        isHealing = false;
        AchievementTracker.Healed();
    }
}
