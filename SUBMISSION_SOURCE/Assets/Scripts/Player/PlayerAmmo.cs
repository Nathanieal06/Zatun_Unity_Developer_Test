// Assets/Scripts/Player/PlayerAmmo.cs
using UnityEngine;

/// <summary>
/// Tracks ammo counts for all ranged weapons on the player.
///
/// Responsibilities:
///   - Store current pistol and rifle ammo totals.
///   - Expose AddAmmo() so AmmoPickup can grant rounds.
///   - Fire GameEvents.RaiseAmmoChanged() after any change so the UI can update.
///
/// Setup (in Inspector):
///   1. Attach to the same GameObject as PlayerEquipment / PickupInteractor.
///   2. Optionally set starting ammo and per-type max via the Inspector fields.
/// </summary>
public class PlayerAmmo : MonoBehaviour
{
    // ── Inspector ────────────────────────────────────────────────────────

    [Header("Pistol Ammo")]
    [Tooltip("Ammo the player starts with")]
    [SerializeField] private int pistolStartAmmo = 0;

    [Tooltip("Maximum pistol ammo the player can carry")]
    [SerializeField] private int pistolMaxAmmo = 120;

    [Header("Rifle Ammo")]
    [Tooltip("Ammo the player starts with")]
    [SerializeField] private int rifleStartAmmo = 0;

    [Tooltip("Maximum rifle ammo the player can carry")]
    [SerializeField] private int rifleMaxAmmo = 300;

    // ── Internal State ───────────────────────────────────────────────────

    private int pistolAmmo;
    private int rifleAmmo;

    // ── Unity Lifecycle ──────────────────────────────────────────────────

    private void Awake()
    {
        pistolAmmo = pistolStartAmmo;
        rifleAmmo  = rifleStartAmmo;
    }

    // ── Public API ───────────────────────────────────────────────────────

    /// <summary>
    /// Called by AmmoPickup when the player picks up ammo.
    /// Adds the given amount to the correct pool, clamping at the maximum.
    /// </summary>
    public void AddAmmo(AmmoType type, int amount)
    {
        if (amount <= 0)
        {
            // Debug.LogWarning($"[PlayerAmmo] AddAmmo called with non-positive amount ({amount}). Ignoring.");
            return;
        }

        switch (type)
        {
            case AmmoType.Pistol:
                pistolAmmo = Mathf.Clamp(pistolAmmo + amount, 0, pistolMaxAmmo);
                // Debug.Log($"[PlayerAmmo] Pistol ammo: {pistolAmmo}/{pistolMaxAmmo}");
                break;

            case AmmoType.Rifle:
                rifleAmmo = Mathf.Clamp(rifleAmmo + amount, 0, rifleMaxAmmo);
                // Debug.Log($"[PlayerAmmo] Rifle ammo: {rifleAmmo}/{rifleMaxAmmo}");
                break;
        }

        GameEvents.RaiseAmmoChanged(type, GetAmmo(type), GetMaxAmmo(type));
    }

    /// <summary>Consume ammo during shooting. Returns false if out of ammo.</summary>
    public bool UseAmmo(AmmoType type, int amount = 1)
    {
        int current = GetAmmo(type);
        if (current < amount) return false;

        switch (type)
        {
            case AmmoType.Pistol: pistolAmmo -= amount; break;
            case AmmoType.Rifle:  rifleAmmo  -= amount; break;
        }

        GameEvents.RaiseAmmoChanged(type, GetAmmo(type), GetMaxAmmo(type));
        return true;
    }

    public int GetAmmo(AmmoType type) => type switch
    {
        AmmoType.Pistol => pistolAmmo,
        AmmoType.Rifle  => rifleAmmo,
        _               => 0
    };

    public int GetMaxAmmo(AmmoType type) => type switch
    {
        AmmoType.Pistol => pistolMaxAmmo,
        AmmoType.Rifle  => rifleMaxAmmo,
        _               => 0
    };

    public bool HasAmmo(AmmoType type) => GetAmmo(type) > 0;
}
