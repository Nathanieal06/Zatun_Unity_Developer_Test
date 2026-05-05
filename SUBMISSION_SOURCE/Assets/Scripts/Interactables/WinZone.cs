using UnityEngine;

/// <summary>
/// Trigger zone that ends the game if the player enters it while carrying the Victory Key.
/// </summary>
public class WinZone : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("If true, the player must have the Victory Key to win.")]
    public bool requiresKey = true;

    private void OnTriggerEnter(Collider other)
    {
        // Check if the object entering the trigger is the Player
        if (other.CompareTag("Player"))
        {
            if (requiresKey)
            {
                // Check only for the specific Victory Key in the inventory
                PlayerInventory inventory = other.GetComponent<PlayerInventory>();
                
                if (inventory != null && inventory.HasVictoryKey)
                {
                    Debug.Log("[WinZone] Player has the VICTORY KEY! Victory!");
                    GameEvents.RaiseGameWin();
                }
                else
                {
                    Debug.Log("[WinZone] Player reached win zone but is missing the VICTORY KEY.");
                    GameEvents.RaiseNarrativeMessage("I need the victory key to leave...", 3f);
                }
            }
            else
            {
                // If requiresKey is false, just trigger the win
                GameEvents.RaiseGameWin();
            }
        }
    }
}
