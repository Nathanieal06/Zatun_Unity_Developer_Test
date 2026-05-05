// Assets/Scripts/Pickups/HealthPickup.cs
using UnityEngine;

/// <summary>
/// Health pickup — restores a fixed amount of health to the player.
/// Demonstrates how to extend BasePickup for any item type.
/// </summary>
public class HealthPickup : BasePickup
{
    [Header("Health Pickup Settings")]
    [SerializeField] private float healAmount = 30f;

    protected override void Awake()
    {
        base.Awake();
        pickupLabel = "Health Pack"; // Override the base label
    }

    public override void OnPickup(GameObject player)
{
    var health = player.GetComponent<PlayerHealth>();

    if (health == null)
    {
        // Debug.LogWarning("[HealthPickup] No PlayerHealth found!");
        return;
    }

    // Don't consume if full — but still show message
    if (health.GetCurrentHealth() >= health.GetMaxHealth())
    {
        // Debug.Log("[HealthPickup] Already full health.");
        return; // Don't consume — pickup stays available
    }

    health.Heal(healAmount);
    // Debug.Log($"[HealthPickup] Healed for {healAmount}.");
    ConsumePickup(); // Only consume when actually used
}
    public override string GetPickupLabel() => "Press F to Pickup";
}