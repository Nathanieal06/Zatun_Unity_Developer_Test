// Assets/Scripts/Pickups/AmmoPickup.cs
using UnityEngine;

/// <summary>
/// A world pickup that grants the player ammo when collected.
///
/// Architecture:
///   - Inherits BasePickup for trigger detection, floating visuals, and the
///     OnPickup(GameObject) contract used by PickupInteractor.
///   - Delegates the actual ammo addition to PlayerAmmo on the picker,
///     keeping this class thin and focused on the pickup event only.
///
/// Setup (in Inspector):
///   1. Set ammoType to Pistol or Rifle.
///   2. Set ammoAmount to how many rounds this pickup grants.
///   3. Attach a Collider (BasePickup's [RequireComponent] enforces this).
///   4. Optionally enable floatInPlace / rotateInPlace for visual flair.
/// </summary>
public class AmmoPickup : BasePickup
{
    [Header("Ammo Pickup Settings")]
    [Tooltip("Which weapon's ammo pool this pickup refills.")]
    public AmmoType ammoType = AmmoType.Pistol;

    [Tooltip("Number of rounds added to the player's ammo pool.")]
    [SerializeField] private int ammoAmount = 60;

    protected override void Awake()
    {
        base.Awake();
        // Build a sensible default label from the ammo type
        pickupLabel = $"{ammoType} Ammo";
    }

    /// <summary>
    /// Returns the HUD prompt shown when the player is near this pickup.
    /// Example: "Press F to Pickup Pistol Ammo"
    /// </summary>
    public override string GetPickupLabel() =>
        $"Press F to Pickup {ammoType} Ammo (+{ammoAmount})";

    /// <summary>
    /// Called by PickupInteractor when the player presses F near this pickup.
    /// Finds PlayerAmmo on the picker and adds the configured rounds.
    /// Only consumed if the player actually needed the ammo.
    /// </summary>
    public override void OnPickup(GameObject picker)
    {
        var playerAmmo = picker.GetComponent<PlayerAmmo>();

        if (playerAmmo == null)
        {
            // Debug.LogWarning($"[AmmoPickup] No PlayerAmmo component found on '{picker.name}'. " +
            //                  "Add PlayerAmmo to the same GameObject as PickupInteractor.");
            return;
        }

        // Don't consume if already at max capacity
        if (playerAmmo.GetAmmo(ammoType) >= playerAmmo.GetMaxAmmo(ammoType))
        {
            // Debug.Log($"[AmmoPickup] {picker.name} already has max {ammoType} ammo. Pickup not consumed.");
            return;
        }

        playerAmmo.AddAmmo(ammoType, ammoAmount);
        // Debug.Log($"[AmmoPickup] '{picker.name}' picked up {ammoAmount}x {ammoType} ammo.");

        ConsumePickup(); // Disable the pickup object (or pool it in the future)
    }
}
