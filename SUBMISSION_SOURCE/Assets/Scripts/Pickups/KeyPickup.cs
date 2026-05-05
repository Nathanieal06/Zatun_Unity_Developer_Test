// Assets/Scripts/Pickups/KeyPickup.cs
using UnityEngine;

/// <summary>
/// Specific pickup for the key.
/// Inherits from BasePickup to use the existing interaction system.
/// </summary>
public class KeyPickup : BasePickup
{
    public enum KeyType { Normal, Victory }
    
    [Header("Key Configuration")]
    [SerializeField] private KeyType keyType = KeyType.Normal;

    public override void OnPickup(GameObject player)
    {
        PlayerInventory inventory = player.GetComponent<PlayerInventory>();
        if (inventory != null)
        {
            if (keyType == KeyType.Normal)
            {
                inventory.AddNormalKey();
            }
            else
            {
                inventory.AddVictoryKey();
            }

            GameEvents.RaiseNarrativeMessage($"{pickupLabel} picked up!", 2f);
            ConsumePickup();
        }
        else
        {
            Debug.LogWarning("[KeyPickup] Player does not have a PlayerInventory component! Make sure it is on the same object as the PickupInteractor.");
        }
    }

    public override string GetPickupLabel()
    {
        return $"Press F to pick up {pickupLabel}";
    }
}
