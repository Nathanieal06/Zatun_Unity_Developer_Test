using UnityEngine;

public class ZombieHitbox : MonoBehaviour
{
    [HideInInspector] public ZombieController zombieController;

    void OnTriggerEnter(Collider other)
    {
        if (zombieController == null || zombieController.currentState != ZombieController.ZombieState.Attacking) 
        {
            return;
        }

        PlayerHealth pHealth = other.GetComponentInParent<PlayerHealth>();
        if (pHealth == null) pHealth = other.GetComponent<PlayerHealth>();
        
        if (other.CompareTag("Player") || pHealth != null)
        {
            if (pHealth != null)
            {
                pHealth.TakeDamage(zombieController.zombieCombat.GetCurrentDamage());
                Debug.Log($"[ZombieHitbox] SUCCESS! Dealt {zombieController.zombieCombat.GetCurrentDamage()} damage to Player!");
                
                // Disable hitbox after successful hit to avoid multi-hitting
                zombieController.DisableHitbox();
            }
            else
            {
                Debug.LogWarning($"[ZombieHitbox] Hit object with Player tag, but could not find PlayerHealth script on it or its parents!");
            }
        }
    }
}
