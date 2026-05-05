// Assets/Scripts/Equipment/Bullet.cs
using UnityEngine;

/// <summary>
/// Attach to your 3D bullet prefab.
/// Moves via Rigidbody velocity (set by ProjectileShooter) for straight projectile travel.
/// Destroys itself on collision with any object.
///
/// MODE:
///   Visual Only = true  -> Bullet is eye candy only. Hitscan handles damage.
///   Visual Only = false -> Bullet deals damage on collision.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class Bullet : MonoBehaviour
{
    [Header("Mode")]
    [Tooltip("Enable this only when a separate hitscan ray should handle all damage.")]
    [SerializeField] private bool visualOnly = false;

    [Header("Damage (ignored when Visual Only is ON)")]
    [Tooltip("Damage dealt to any object with PlayerHealth, ZombieController, or ZombieHealth.")]
    [SerializeField] private float damage = 25f;

    [Header("Impact Effects")]
    [Tooltip("Optional particle prefab spawned at the hit point.")]
    [SerializeField] private GameObject impactEffectPrefab;

    [Tooltip("How long the impact effect lives before being destroyed.")]
    [SerializeField] private float impactEffectLifetime = 1f;

    [Header("Trail")]
    [Tooltip("Optional TrailRenderer on the bullet. Detached on impact so it fades out naturally.")]
    [SerializeField] private TrailRenderer bulletTrail;

    private Rigidbody _rb;
    private bool _hasHit;

    public void Configure(float projectileDamage, bool isVisualOnly)
    {
        damage = projectileDamage;
        visualOnly = isVisualOnly;
    }

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        if (_rb == null)
        {
            Debug.LogError("[Bullet] No Rigidbody on bullet prefab! It will not move.");
            return;
        }

        _rb.isKinematic = false;
        _rb.useGravity = false;
        _rb.linearDamping = 0f;
        _rb.angularDamping = 0f;
        _rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (_hasHit) return;

        Vector3 hitPoint = (collision.contactCount > 0) ? collision.GetContact(0).point : transform.position;
        Vector3 hitNormal = (collision.contactCount > 0) ? collision.GetContact(0).normal : -transform.forward;

        ProcessHit(collision.collider, hitPoint, hitNormal);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_hasHit) return;

        // Ignore trigger volumes unless they belong to a damageable target.
        if (TryGetDamageable(other, out _, out _, out _))
        {
            ProcessHit(other, transform.position, -transform.forward);
        }
    }

    private void ProcessHit(Collider other, Vector3 hitPoint, Vector3 hitNormal)
    {
        if (_hasHit) return;
        _hasHit = true;

        if (!visualOnly)
        {
            DealDamage(other);
        }

        if (impactEffectPrefab != null)
        {
            GameObject effect = Instantiate(
                impactEffectPrefab,
                hitPoint,
                Quaternion.LookRotation(hitNormal));
            Destroy(effect, impactEffectLifetime);
        }

        if (bulletTrail != null)
        {
            bulletTrail.transform.SetParent(null);
            Destroy(bulletTrail.gameObject, bulletTrail.time);
        }

        Destroy(gameObject);
    }

    private void DealDamage(Collider other)
    {
        if (!TryGetDamageable(other, out PlayerHealth playerHealth, out ZombieController zombie, out ZombieHealth zombieHealth))
            return;

        if (zombie != null)
        {
            zombie.TakeDamage(damage);
            Debug.Log($"[Bullet] Hit Zombie {zombie.gameObject.name} via {other.name} for {damage} damage.");
            return;
        }

        if (zombieHealth != null)
        {
            zombieHealth.TakeDamage(damage);
            Debug.Log($"[Bullet] Hit ZombieHealth {zombieHealth.gameObject.name} via {other.name} for {damage} damage.");
            return;
        }

        playerHealth.TakeDamage(damage);
        Debug.Log($"[Bullet] Hit Player for {damage} damage.");
    }

    private bool TryGetDamageable(
        Collider other,
        out PlayerHealth playerHealth,
        out ZombieController zombie,
        out ZombieHealth zombieHealth)
    {
        playerHealth = other.GetComponentInParent<PlayerHealth>();
        if (playerHealth == null) playerHealth = other.GetComponentInChildren<PlayerHealth>();

        zombie = other.GetComponentInParent<ZombieController>();
        if (zombie == null) zombie = other.GetComponentInChildren<ZombieController>();

        zombieHealth = other.GetComponentInParent<ZombieHealth>();
        if (zombieHealth == null) zombieHealth = other.GetComponentInChildren<ZombieHealth>();

        return playerHealth != null || zombie != null || zombieHealth != null;
    }
}
