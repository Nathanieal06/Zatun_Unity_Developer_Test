// Assets/Scripts/Equipment/ProjectileShooter.cs
using UnityEngine;

/// <summary>
/// Fires a physical bullet from the ACTIVE weapon's muzzle only.
/// Subscribes to GameEvents.OnWeaponEquipped to know which weapon is equipped.
/// When WeaponType.None → all firing is blocked.
///
/// Muzzle point is resolved from the active WeaponBase child (the weapon prefab's
/// own MuzzlePoint), so the bullet always exits from the correct barrel tip.
///
/// Muzzle flash is handled entirely by WeaponBase.Attack() — no duplicate flash here.
/// Assign the muzzleFlashPrefab on each weapon prefab (WeaponPistol / WeaponRifle)
/// and it will automatically play at the correct muzzle point every shot.
/// </summary>
public class ProjectileShooter : MonoBehaviour
{
    [Header("Projectiles")]
    [Tooltip("Pistol bullet prefab (must have Bullet.cs and Rigidbody).")]
    [SerializeField] private GameObject pistolBulletPrefab;

    [Tooltip("Rifle bullet prefab (must have Bullet.cs and Rigidbody).")]
    [SerializeField] private GameObject rifleBulletPrefab;

    [Tooltip("Speed of the bullet in units/sec. 800-1200 is realistic for a pistol.")]
    [SerializeField] private float bulletSpeed = 1000f;

    [Tooltip("Seconds before the bullet auto-destroys if it hasn't hit anything.")]
    [SerializeField] private float bulletLifetime = 5f;

    [Header("Muzzle Points (Legacy — leave blank, weapon prefab supplies them)")]
    [Tooltip("LEGACY ONLY: Used only when the active WeaponBase has no MuzzlePoint assigned. " +
             "Prefer assigning Muzzle Point on the weapon prefab itself.")]
    [SerializeField] private Transform pistolMuzzlePoint;

    [Tooltip("LEGACY ONLY: Used only when the active WeaponBase has no MuzzlePoint assigned. " +
             "Prefer assigning Muzzle Point on the weapon prefab itself.")]
    [SerializeField] private Transform rifleMuzzlePoint;

    [Tooltip("How far in front of the muzzle to spawn the bullet (prevents own-collider overlap).")]
    [SerializeField] private float muzzleSpawnOffset = 0.2f;

    [Header("Physics Layers")]
    [Tooltip("Layer index of the Bullet layer.")]
    [SerializeField] private int bulletLayer = 8;

    [Tooltip("Layer index of the Player layer. Bullets never collide with this.")]
    [SerializeField] private int playerLayer = 6;

    [Header("State (read-only in Play Mode)")]
    [Tooltip("Which weapon is currently equipped. Driven by GameEvents.OnWeaponEquipped.")]
    [SerializeField] private WeaponType currentWeapon = WeaponType.None;

    private float _lastFireTime;

    // ── Lifecycle ──────────────────────────────────────────────────────────

    private void Start()
    {
        // Ignore bullet<>player layer collisions globally so bullets never
        // hit the player's own colliders regardless of spawn timing.
        Physics.IgnoreLayerCollision(bulletLayer, playerLayer, true);
    }

    private void OnEnable()
    {
        GameEvents.OnShotFired      += HandleShotFired;
        GameEvents.OnWeaponEquipped += HandleWeaponEquipped;
    }

    private void OnDisable()
    {
        GameEvents.OnShotFired      -= HandleShotFired;
        GameEvents.OnWeaponEquipped -= HandleWeaponEquipped;
    }

    // ── Weapon tracking ────────────────────────────────────────────────────

    private void HandleWeaponEquipped(WeaponType type)
    {
        currentWeapon = type;
    }

    /// <summary>
    /// Public API: lets external code (e.g. weapon pickup) set the weapon directly.
    /// </summary>
    public void SetWeapon(WeaponType type)
    {
        currentWeapon = type;
    }

    // ── Firing Logic ───────────────────────────────────────────────────────

    private void HandleShotFired(WeaponType firedWeapon)
    {
        // Only handle the event for the currently equipped weapon.
        if (firedWeapon != currentWeapon) return;

        // Block firing if no weapon is equipped.
        if (currentWeapon == WeaponType.None)  return;
        if (currentWeapon == WeaponType.Knife) return; // knives don't fire bullets

        GameObject bulletPrefabToUse = currentWeapon == WeaponType.Rifle
            ? rifleBulletPrefab
            : pistolBulletPrefab;

        if (bulletPrefabToUse == null)
        {
            Debug.LogWarning($"[ProjectileShooter] No bullet prefab assigned for {currentWeapon}.");
            return;
        }

        // ── Resolve muzzle — comes from the weapon prefab's own MuzzlePoint ──
        Transform muzzle = ResolveMuzzle();
        if (muzzle == null)
        {
            Debug.LogWarning($"[ProjectileShooter] No muzzle point found for {currentWeapon}. " +
                             "Assign 'Muzzle Point' on the WeaponPistol / WeaponRifle component in the Inspector.");
            return;
        }

        _lastFireTime = Time.time;

        // ── Direction (fresh every shot — never cached) ────────────────────
        Vector3 targetPoint = UI.WorldCrosshairController.CurrentTargetPoint;
        if (targetPoint == Vector3.zero)
            targetPoint = muzzle.position + muzzle.forward * 200f;

        Vector3 direction = (targetPoint - muzzle.position).normalized;
        if (direction == Vector3.zero) direction = muzzle.forward;

        // ── Spawn bullet slightly in front of the muzzle ──────────────────
        Vector3    spawnPos = muzzle.position + direction * muzzleSpawnOffset;
        Quaternion spawnRot = Quaternion.LookRotation(direction);

        GameObject bulletObj = Instantiate(bulletPrefabToUse, spawnPos, spawnRot);

        // Ensure bullet NEVER hits the player who shot it regardless of layers.
        Collider bulletCol = bulletObj.GetComponentInChildren<Collider>();
        if (bulletCol != null)
        {
            Collider[] playerColliders = transform.root.GetComponentsInChildren<Collider>();
            foreach (Collider pc in playerColliders)
            {
                if (pc != bulletCol) Physics.IgnoreCollision(bulletCol, pc, true);
            }
        }

        // Set bullet layer before the physics tick.
        bulletObj.layer = bulletLayer;
        foreach (Transform child in bulletObj.transform)
            child.gameObject.layer = bulletLayer;

        // ── Rigidbody — force correct settings and apply velocity ──────────
        Rigidbody rb = bulletObj.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic            = false;
            rb.useGravity             = false;
            rb.linearDamping          = 0f;
            rb.angularDamping         = 0f;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            rb.linearVelocity         = direction * bulletSpeed;
        }
        else
        {
            Debug.LogError("[ProjectileShooter] Bullet prefab has NO Rigidbody!");
        }

        Destroy(bulletObj, bulletLifetime);

        // ── Muzzle flash ───────────────────────────────────────────────────
        // WeaponBase.Attack() already spawns the muzzleFlashPrefab parented at
        // the weapon's own muzzlePoint before this event fires, so the flash is
        // already playing at the correct barrel tip.
        // Do NOT spawn a second flash here — that would cause a duplicate effect
        // at a potentially wrong position.
    }

    // ── Muzzle resolution ──────────────────────────────────────────────────

    /// <summary>
    /// Returns the muzzle Transform for the currently equipped weapon.
    ///
    /// Priority:
    ///   1. Active WeaponBase child whose WeaponKind matches — reads MuzzlePoint
    ///      directly from the weapon prefab (single source of truth).
    ///   2. Legacy inspector slots (pistolMuzzlePoint / rifleMuzzlePoint) for
    ///      backward-compatibility with older scene setups.
    /// </summary>
    private Transform ResolveMuzzle()
    {
        // 1. Ask the currently active WeaponBase for its own muzzle point.
        WeaponBase[] weapons = GetComponentsInChildren<WeaponBase>(includeInactive: false);
        foreach (WeaponBase wb in weapons)
        {
            if (wb.WeaponKind == currentWeapon && wb.MuzzlePoint != null)
                return wb.MuzzlePoint;
        }

        // 2. Fallback to legacy inspector-assigned slots.
        switch (currentWeapon)
        {
            case WeaponType.Pistol:
                if (pistolMuzzlePoint != null) return pistolMuzzlePoint;
                break;

            case WeaponType.Rifle:
                if (rifleMuzzlePoint != null) return rifleMuzzlePoint;
                break;
        }

        return null;
    }
}
