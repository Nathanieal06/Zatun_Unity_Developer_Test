// Assets/Scripts/Equipment/WeaponRifle.cs
using UnityEngine;

/// <summary>
/// Assault Rifle weapon — auto fire will be added Day 3.
/// </summary>
public class WeaponRifle : WeaponBase
{
    [Header("Hitscan Settings")]
    [SerializeField] private float range = 80f;
    [SerializeField] private LayerMask hitMask;

    protected override void Awake()
    {
        // Force correct settings to prevent Inspector setup errors
        usesAmmo = true;
        ammoType = AmmoType.Rifle;
        if (maxMagazine <= 0) maxMagazine = 30;

        base.Awake();
        attackRate = 0.25f; // Fast fire rate (auto)

        // If hitMask is 0 (Nothing), set it to Everything so raycasts actually work
        if (hitMask == 0) hitMask = ~0;
    }

    public override bool Attack(Transform origin)
    {
        // Enforce fire rate first to prevent spamming
        if (Time.time - lastAttackTime < attackRate) return false;

        // Check ammo before triggering base attack (which plays FX/animations)
        if (currentMagazine <= 0)
        {
            lastAttackTime = Time.time;
            GameEvents.RaiseAttackBlocked("Out of ammo. Press R to reload.");
            return false;
        }

        // Try base attack
        if (!base.Attack(origin)) return false;

        // Fire single shot of the auto burst
        currentMagazine--;
        GameEvents.RaiseMagazineChanged(WeaponKind, currentMagazine, maxMagazine);
        GameEvents.RaiseShotFired(WeaponKind);

        Debug.Log($"[Rifle] Ratatat! Ammo left in mag: {currentMagazine}/{maxMagazine}");

        // ── Hitscan ────────────────────────────────────────────────────────
        // Use the world-space target already computed each frame by WorldCrosshairController.
        // This avoids a redundant camera raycast and ensures the bullet path exactly
        // matches the on-screen crosshair position.
        Vector3 targetPoint = UI.WorldCrosshairController.CurrentTargetPoint;

        // Fallback: if the crosshair target is at zero (not initialized) aim along camera forward.
        if (targetPoint == Vector3.zero)
            targetPoint = origin.position + origin.forward * range;

        // Stage 2: Fire from the muzzle toward the target point.
        //          Falls back to camera origin if no muzzle point is assigned.
        Vector3 fireOrigin    = (muzzlePoint != null) ? muzzlePoint.position : origin.position;
        Vector3 fireDirection = (targetPoint - fireOrigin).normalized;

        Debug.DrawRay(fireOrigin, fireDirection * range, Color.red, 1f);

        // Use QueryTriggerInteraction.Collide so we can hit the little zombie's trigger collider
        RaycastHit[] muzzleHits = Physics.RaycastAll(fireOrigin, fireDirection, range, hitMask, QueryTriggerInteraction.Collide);
        System.Array.Sort(muzzleHits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (var hit in muzzleHits)
        {
            if (playerRoot != null && hit.transform.root == playerRoot) continue;

            // Look for ZombieHealth or PlayerHealth
            var zHealth = hit.collider.GetComponentInParent<ZombieHealth>();
            var pHealth = hit.collider.GetComponentInParent<PlayerHealth>();

            if (zHealth != null)
            {
                zHealth.TakeDamage(damage);
                break; // Stop at the zombie
            }
            else if (pHealth != null)
            {
                pHealth.TakeDamage(damage);
                break; // Stop at the player
            }
            else if (!hit.collider.isTrigger)
            {
                // If it's a solid object (wall/ground), stop the bullet
                break; 
            }
            
            // If it's just a regular trigger (not a zombie), the bullet passes through
        }

        return true;
    }
}