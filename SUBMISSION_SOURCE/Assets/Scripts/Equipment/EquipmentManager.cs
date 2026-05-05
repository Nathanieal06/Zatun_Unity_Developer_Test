// Assets/Scripts/Equipment/EquipmentManager.cs
using UnityEngine;

/// <summary>
/// Manages player's equipped weapon.
/// Controls equip, unequip, and drop logic.
/// </summary>
public class EquipmentManager : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Child transform where weapons are held")]
    [SerializeField] private Transform weaponHolder;

    [Header("Weapon Prefabs")]
    [SerializeField] private WeaponBase knifePrefab;
    [SerializeField] private WeaponBase pistolPrefab;
    [SerializeField] private WeaponBase riflePrefab;

    [Header("Drop Settings")]
    [SerializeField] private float dropDistance = 1.5f;
    [SerializeField] private float dropUpwardForce = 2f;

    // Internal state
    private WeaponBase currentWeapon;
    private WeaponBase knifeInstance;
    private WeaponBase pistolInstance;
    private WeaponBase rifleInstance;

    private void OnEnable() { }

    private void OnDisable() { }

    private void Start()
    {
        Debug.Log("[EquipmentManager] Starting...");

        if (weaponHolder == null)
        {
            Debug.LogError("[EquipmentManager] WeaponHolder is NULL! Assign it in Inspector.");
            return;
        }

        // If no prefabs are assigned this component is inactive (PlayerEquipment
        // handles equipment instead). Skip silently to avoid console spam.
        if (knifePrefab == null && pistolPrefab == null && riflePrefab == null)
        {
            Debug.Log("[EquipmentManager] No prefabs assigned — running in passive mode.");
            return;
        }

        // Spawn all weapons hidden
        if (knifePrefab != null)
        {
            knifeInstance = SpawnWeapon(knifePrefab);
            Debug.Log("[EquipmentManager] Knife spawned");
        }

        if (pistolPrefab != null)
        {
            pistolInstance = SpawnWeapon(pistolPrefab);
            Debug.Log("[EquipmentManager] Pistol spawned");
        }

        if (riflePrefab != null)
        {
            rifleInstance = SpawnWeapon(riflePrefab);
            Debug.Log("[EquipmentManager] Rifle spawned");
        }

        currentWeapon = null;
        Debug.Log("[EquipmentManager] Ready. No weapon equipped.");
    }

    // ── Private Helpers ───────────────────────────────────────────────

    private WeaponBase SpawnWeapon(WeaponBase prefab)
    {
        var instance = Instantiate(prefab, weaponHolder);
        instance.transform.localPosition = Vector3.zero;
        instance.transform.localRotation = Quaternion.identity;
        instance.gameObject.SetActive(false);
        return instance;
    }

    private void HandleWeaponSwitch(int index)
    {
        Debug.Log($"[EquipmentManager] Switch called: {index}");

        WeaponBase target = index switch
        {
            0 => null,          // 1 = No weapon
            1 => knifeInstance, // 2 = Knife
            2 => pistolInstance,// 3 = Pistol
            3 => rifleInstance, // 4 = Rifle
            _ => null
        };

        Debug.Log($"[EquipmentManager] Target: {(target != null ? target.WeaponName : "None")}");
        EquipWeapon(target);
    }

    private void EquipWeapon(WeaponBase newWeapon)
    {
        // Unequip current weapon first
        if (currentWeapon != null)
        {
            currentWeapon.OnUnequip();
        }

        // Equip new weapon
        currentWeapon = newWeapon;

        if (currentWeapon != null)
        {
            currentWeapon.OnEquip();
            // NOTE: Do NOT raise RaiseWeaponEquipped here.
            // PlayerEquipment (the inventory owner) raises it after calling EquipByTypePublic.
            // Raising it twice causes duplicate UI updates and double WeaponController init.
        }
        else
        {
            Debug.Log("[EquipmentManager] No weapon equipped.");
        }
    }

    private void HandleDrop()
    {
        if (currentWeapon == null)
        {
            Debug.Log("[EquipmentManager] Nothing to drop.");
            return;
        }

        // Spawn world prefab
        if (currentWeapon.DroppedPrefab != null)
        {
            Vector3 dropPos = transform.position
                            + transform.forward * dropDistance
                            + Vector3.up * 0.5f;

            var dropped = Instantiate(
                currentWeapon.DroppedPrefab,
                dropPos,
                transform.rotation
            );

            // Add physics force
            var rb = dropped.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.AddForce(
                    transform.forward * dropDistance
                    + Vector3.up * dropUpwardForce,
                    ForceMode.VelocityChange
                );
            }
        }
        else
        {
            Debug.LogWarning($"[EquipmentManager] {currentWeapon.WeaponName} has no DroppedPrefab!");
        }

        Debug.Log($"[EquipmentManager] Dropped: {currentWeapon.WeaponName}");
        // NOTE: PlayerEquipment raises RaiseWeaponDropped — don't duplicate it here.

        currentWeapon.OnUnequip();
        currentWeapon = null;
    }

    // ── Public API ────────────────────────────────────────────────────────

    public WeaponBase GetCurrentWeapon() => currentWeapon;
    public bool HasWeapon() => currentWeapon != null;

    /// <summary>
    /// Public wrapper so PlayerEquipment can request a weapon switch by type.
    /// Passing WeaponType.None equips nothing (unarmed).
    /// </summary>
    public void EquipByTypePublic(WeaponType type)
    {
        WeaponBase target = type switch
        {
            WeaponType.Knife   => knifeInstance,
            WeaponType.Pistol  => pistolInstance,
            WeaponType.Rifle   => rifleInstance,
            _                  => null
        };
        EquipWeapon(target);
    }

    /// <summary>
    /// Public wrapper so PlayerEquipment can trigger a drop without
    /// re-implementing the spawn and physics logic here.
    /// </summary>
    public void HandleDropPublic() => HandleDrop();

    /// <summary>
    /// Called by weapon pickups to give player a weapon.
    /// </summary>
    public void AddWeapon(WeaponType type)
    {
        Debug.Log($"[EquipmentManager] Adding weapon: {type}");

        switch (type)
        {
            case WeaponType.Knife:
                if (knifeInstance != null) EquipWeapon(knifeInstance);
                break;
            case WeaponType.Pistol:
                if (pistolInstance != null) EquipWeapon(pistolInstance);
                break;
            case WeaponType.Rifle:
                if (rifleInstance != null) EquipWeapon(rifleInstance);
                break;
        }
    }
}