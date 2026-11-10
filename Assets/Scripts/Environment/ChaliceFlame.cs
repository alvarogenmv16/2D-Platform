using UnityEngine;

// One chalice of a ChaliceSequence puzzle. Struck by the player's sword
// like any breakable (PlayerAttackHitbox calls IBreakable.Break), but it
// never actually breaks: a strike just asks the parent ChaliceSequence
// whether this was the right chalice, and the sequence decides whether to
// light or snuff the flame. All the puzzle logic lives in the sequence -
// this only shows/hides its flame.
//
// Setup: child of the GameObject holding the ChaliceSequence, on the
// Breakable layer, with a trigger Collider2D. The flame is a child
// GameObject (sprite + flicker Animator, optionally a Light2D), toggled on
// and off as a whole.
[RequireComponent(typeof(Collider2D))]
public class ChaliceFlame : MonoBehaviour, IBreakable
{
    [SerializeField] private GameObject flame;

    private ChaliceSequence sequence;

    public bool IsLit { get; private set; }

    private void Awake()
    {
        sequence = GetComponentInParent<ChaliceSequence>();

        if (sequence == null)
        {
            Debug.LogWarning($"{name}: ChaliceFlame needs a ChaliceSequence on a parent object.", this);
        }

        SetLit(false);
    }

    // IBreakable: called by PlayerAttackHitbox when the sword hits.
    public void Break()
    {
        sequence?.OnChaliceStruck(this);
    }

    public void SetLit(bool lit)
    {
        IsLit = lit;

        if (flame != null)
        {
            flame.SetActive(lit);
        }
    }
}
