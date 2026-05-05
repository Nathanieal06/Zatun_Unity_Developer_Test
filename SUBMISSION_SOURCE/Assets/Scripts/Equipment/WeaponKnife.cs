// Assets/Scripts/Equipment/WeaponKnife.cs
using UnityEngine;

/// <summary>
/// Knife weapon — melee attack will be added Day 3.
/// </summary>
public class WeaponKnife : WeaponBase
{
    [Header("Melee Settings")]
    [SerializeField] private float attackRadius = 1.5f;
    [SerializeField] private float attackForwardOffset = 1f;
    [SerializeField] private LayerMask hitMask;

    // Cached reference to the trigger-based hitbox on the knife model
    private KnifeHitbox knifeHitbox;

    protected override void Awake()
    {
        base.Awake();
        usesAmmo = false;
        attackRate = 0.8f;
        knifeHitbox = GetComponentInChildren<KnifeHitbox>();
    }

    public override bool Attack(Transform origin)
    {
        if (!base.Attack(origin)) return false;

        Debug.Log("[Knife] Swipe!");

        // If a KnifeHitbox is attached, use it (trigger-based, more reliable)
        if (knifeHitbox != null)
        {
            knifeHitbox.EnableHitbox();
            // Disable the hitbox after the active swing window (half of attackRate)
            Invoke(nameof(DisableKnifeHitbox), attackRate * 0.5f);
            return true;
        }

        // In a real game, you'd trigger an animation here, and the animation
        // event would call the actual damage logic. For this test, we do it instantly.

        // Use the camera/origin forward to compute attack center
        Vector3 hitCenter = origin.position + origin.forward * attackForwardOffset;

        // Use ~0 (Everything) if hitMask was left unset in Inspector
        LayerMask mask = (hitMask == 0) ? ~0 : hitMask;

        // QueryTriggerInteraction.Collide is REQUIRED — zombie hitboxes are triggers!
        Collider[] hits = Physics.OverlapSphere(hitCenter, attackRadius, mask, QueryTriggerInteraction.Collide);

        Debug.Log($"[Knife] Attack! Center: {hitCenter}, Radius: {attackRadius}, Hits found: {hits.Length}");

        foreach (var hit in hits)
        {
            if (playerRoot != null && hit.transform.root == playerRoot)
                continue;

            // 1. Try finding ZombieController
            var zombie = hit.GetComponentInParent<ZombieController>();
            if (zombie == null) zombie = hit.GetComponentInChildren<ZombieController>();

            // 2. Try finding ZombieHealth directly (as fallback)
            var zHealth = hit.GetComponentInParent<ZombieHealth>();
            if (zHealth == null) zHealth = hit.GetComponentInChildren<ZombieHealth>();

            // 3. Try finding PlayerHealth
            var playerHealth = hit.GetComponentInParent<PlayerHealth>();
            if (playerHealth == null) playerHealth = hit.GetComponentInChildren<PlayerHealth>();

            if (zombie != null)
            {
                Debug.Log($"[Knife] Hit Zombie ({zombie.gameObject.name}) on {hit.name}! Dealing {damage} damage.");
                zombie.TakeDamage(damage);
            }
            else if (zHealth != null)
            {
                Debug.Log($"[Knife] Hit ZombieHealth on {hit.name}! Dealing {damage} damage.");
                zHealth.TakeDamage(damage);
            }
            else if (playerHealth != null)
            {
                Debug.Log($"[Knife] Hit Player on {hit.name}! Dealing {damage} damage.");
                playerHealth.TakeDamage(damage);
            }
            else
            {
                Debug.Log($"[Knife] Hit {hit.name} (Layer: {LayerMask.LayerToName(hit.gameObject.layer)}), but no target found.");
            }
        }

        return true;
    }

    private void DisableKnifeHitbox()
    {
        if (knifeHitbox != null) knifeHitbox.DisableHitbox();
    }

    /// <summary>Returns the damage value so KnifeHitbox can read it.</summary>
    public float GetDamage() => damage;

    // Optional: Draw debug sphere to see the attack range in editor
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Transform origin = transform;
        
        // Try to use camera if in play mode, otherwise use weapon's transform (which is attached to player)
        if (Application.isPlaying && Camera.main != null)
        {
            origin = Camera.main.transform;
        }

        Vector3 hitCenter = origin.position + origin.forward * attackForwardOffset;
        Gizmos.DrawWireSphere(hitCenter, attackRadius);
    }
}