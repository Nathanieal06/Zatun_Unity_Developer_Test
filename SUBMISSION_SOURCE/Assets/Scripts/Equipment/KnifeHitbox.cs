// Assets/Scripts/Equipment/KnifeHitbox.cs
using UnityEngine;

/// <summary>
/// Attach this to the Knife GameObject (which has the Box Collider trigger).
/// Enables/disables itself during the swing animation so it only damages
/// on an actual attack, not while the knife is idle at the player's side.
/// </summary>
public class KnifeHitbox : MonoBehaviour
{
    [Tooltip("Damage dealt per hit. Pulled from WeaponKnife automatically if left at 0.")]
    [SerializeField] private float damage = 35f;

    [Tooltip("Maximum distance from player to zombie for the hit to register.")]
    [SerializeField] private float maxAttackRange = 2f;

    private Collider hitCollider;
    private WeaponKnife knife;
    private Transform playerTransform;

    // Tracks which zombies have already been hit this swing to avoid double damage
    private System.Collections.Generic.HashSet<ZombieHealth> hitThisSwing
        = new System.Collections.Generic.HashSet<ZombieHealth>();

    private void Awake()
    {
        hitCollider = GetComponent<Collider>();
        knife = GetComponentInParent<WeaponKnife>();

        // Cache the player transform for distance checks
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) playerTransform = player.transform;

        // Disable hitbox at start — only enabled during a swing
        if (hitCollider != null)
        {
            hitCollider.isTrigger = true;
            hitCollider.enabled = false;
        }
    }

    /// <summary>Call this from WeaponKnife.Attack() or an Animation Event to start a swing.</summary>
    public void EnableHitbox()
    {
        hitThisSwing.Clear();
        if (hitCollider != null) hitCollider.enabled = true;
    }

    /// <summary>Call this to end the swing window (e.g. after attackRate seconds).</summary>
    public void DisableHitbox()
    {
        if (hitCollider != null) hitCollider.enabled = false;
        hitThisSwing.Clear();
    }

    private void OnTriggerEnter(Collider other)
    {
        // Skip the player themselves
        if (other.CompareTag("Player")) return;

        // Find zombie health on the collider or its parents
        ZombieHealth zHealth = other.GetComponentInParent<ZombieHealth>();
        if (zHealth == null) zHealth = other.GetComponent<ZombieHealth>();

        if (zHealth != null)
        {
            // ── Range Check ──────────────────────────────────────────────
            // Only deal damage if the player is within close melee range.
            // This prevents trigger ghost-hits when the zombie walks into
            // the collider without the player actively being close.
            if (playerTransform != null)
            {
                float dist = Vector3.Distance(playerTransform.position, zHealth.transform.position);
                if (dist > maxAttackRange)
                {
                    Debug.Log($"[KnifeHitbox] Too far ({dist:F1}m > {maxAttackRange}m) — no damage.");
                    return;
                }
            }

            // Prevent hitting the same zombie multiple times in one swing
            if (hitThisSwing.Contains(zHealth)) return;
            hitThisSwing.Add(zHealth);

            float dmg = (knife != null) ? knife.GetDamage() : damage;
            Debug.Log($"[KnifeHitbox] Hit {zHealth.gameObject.name} for {dmg} damage!");
            zHealth.TakeDamage(dmg);
        }
    }
}
