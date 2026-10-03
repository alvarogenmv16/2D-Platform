// Anything PlayerAttackHitbox can break in one hit (KeyBlock, FakeKeyBlock,
// and any future variant) - keeps PlayerAttackHitbox from needing a new
// GetComponent check for every new breakable type added later.
public interface IBreakable
{
    void Break();
}
