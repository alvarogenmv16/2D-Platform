// Marks anything that damages the player on contact (HazardDamage tiles,
// spikes, lasers, RisingFlameSea...). PlayerRespawnPoint uses it to never
// record a "safe" position right next to one - so a bump-back can't drop
// the player back into the thing that just hurt them. Marker only: each
// hazard still deals its own damage its own way.
public interface IHazard
{
}
