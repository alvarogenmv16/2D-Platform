using UnityEngine;
using UnityEngine.UI;

// Fixed HUD boss health bar. Hidden until the fight starts (BossArenaController
// calls Show() on trigger enter), fills down as the boss takes damage, hides
// again once the boss dies. Same OnHealthChanged subscription pattern as
// PlayerHealthUI, just a single fill bar instead of per-mask icons.
public class BossHealthUI : MonoBehaviour
{
    // =========================
    // VARIABLES
    // =========================

    [SerializeField] private EnemyHealth bossHealth;
    [SerializeField] private GameObject barRoot;
    [SerializeField] private Image fillImage; // Image.Type = Filled

    // =========================
    // START
    // =========================
    private void Awake()
    {
        if (barRoot != null)
        {
            barRoot.SetActive(false);
        }
    }

    private void OnEnable()
    {
        if (bossHealth != null)
        {
            bossHealth.OnHealthChanged.AddListener(UpdateFill);
            bossHealth.OnDied.AddListener(Hide);
        }
    }

    private void OnDisable()
    {
        if (bossHealth != null)
        {
            bossHealth.OnHealthChanged.RemoveListener(UpdateFill);
            bossHealth.OnDied.RemoveListener(Hide);
        }
    }

    // =========================
    // FUNCTIONS
    // =========================

    public void Show()
    {
        if (barRoot != null)
        {
            barRoot.SetActive(true);
        }
    }

    public void Hide()
    {
        if (barRoot != null)
        {
            barRoot.SetActive(false);
        }
    }

    private void UpdateFill(float currentHealth, float maxHealth)
    {
        if (fillImage == null) return;

        fillImage.fillAmount = maxHealth > 0f ? currentHealth / maxHealth : 0f;
    }
}
