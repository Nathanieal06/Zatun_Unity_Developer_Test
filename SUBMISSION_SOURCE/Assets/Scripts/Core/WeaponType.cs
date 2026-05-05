// Assets/Scripts/Core/WeaponType.cs
// No 'using' directives needed — it's just an enum

/// <summary>
/// Shared enum for all weapon types.
/// Lives in Core/ because it is referenced by GameEvents, EquipmentManager,
/// and the Pickup system — it belongs to no single system.
/// </summary>
public enum WeaponType
{
    None   = 0,
    Knife  = 1,
    Pistol = 2,
    Rifle  = 3
}