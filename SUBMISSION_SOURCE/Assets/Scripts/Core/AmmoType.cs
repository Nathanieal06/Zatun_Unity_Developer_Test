// Assets/Scripts/Core/AmmoType.cs

/// <summary>
/// Identifies which weapon's ammo pool a pickup or event refers to.
/// Lives in Core/ because it is shared by AmmoPickup, PlayerAmmo, and GameEvents.
/// </summary>
public enum AmmoType
{
    Pistol = 0,
    Rifle  = 1
}
