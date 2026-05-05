using UnityEngine;

/// <summary>
/// Attach this script to your Water object (which should have a BoxCollider set to 'Is Trigger').
/// When the player touches this collider, they will die instantly and show the death panel.
/// </summary>
public class WaterDeath : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        // Check if the object entering the water is the Player
        if (other.CompareTag("Player") || other.GetComponentInParent<PlayerHealth>() != null)
        {
            PlayerHealth health = other.GetComponent<PlayerHealth>();
            if (health == null) health = other.GetComponentInParent<PlayerHealth>();
            
            if (health != null)
            {
                Debug.Log("[WaterDeath] Player entered water! Triggering instant death.");
                
                // Deal massive damage to trigger the death screen
                health.TakeDamage(9999f);
            }
        }
    }
}
