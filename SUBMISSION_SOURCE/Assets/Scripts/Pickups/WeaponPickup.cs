// Assets/Scripts/Pickups/WeaponPickup.cs
using UnityEngine;

/// <summary>
/// A world pickup that grants the player a weapon when collected.
/// 
/// Architecture:
///   - Inherits BasePickup for trigger detection, floating visuals, and the
///     OnPickup(GameObject) contract used by PickupInteractor.
///   - Delegates actual inventory logic to PlayerEquipment on the picker,
///     keeping this class thin and focused on the pickup event only.
/// 
/// Setup (in Inspector):
///   1. Assign the WeaponType this pickup represents.
///   2. Attach a Collider (BasePickup's [RequireComponent] enforces this).
///   3. Optionally set pickupLabel for a custom UI prompt.
/// </summary>
public class WeaponPickup : BasePickup
{
    [Header("Weapon Pickup Settings")]
    [Tooltip("The weapon type this pickup will unlock for the player.")]
    public WeaponType weaponType;

    protected override void Awake()
    {
        base.Awake();
        // Build a sensible default label from the weapon type name
        pickupLabel = weaponType.ToString();
    }

    /// <summary>
    /// Returns the prompt string shown in the HUD when the player is nearby.
    /// Example: "Press F to Pickup Rifle"
    /// </summary>
    public override string GetPickupLabel() =>
        $"Press F to Pickup {weaponType}";

    /// <summary>
    /// Called by PickupInteractor when the player presses F near this pickup.
    /// Finds PlayerEquipment on the picker and unlocks the weapon.
    /// </summary>
    public override void OnPickup(GameObject picker)
    {
        var equipment = picker.GetComponent<PlayerEquipment>();

        if (equipment == null)
        {
            // Debug.LogWarning($"[WeaponPickup] No PlayerEquipment found on '{picker.name}'. " +
            //                  "Make sure PlayerEquipment is on the same GameObject as PickupInteractor.");
            return;
        }

        // Capture world rotation before this object is destroyed so the
        // drop can restore the weapon to exactly the same orientation.
        Quaternion pickupRotation = transform.rotation;

        equipment.UnlockWeapon(weaponType, pickupRotation);
        // Debug.Log($"[WeaponPickup] '{picker.name}' picked up {weaponType}.");

        Destroy(gameObject);
    }
}