// Assets/Scripts/Equipment/WeaponBase.cs
using UnityEngine;

/// <summary>
/// Base class for all equippable weapons.
/// </summary>
public abstract class WeaponBase : MonoBehaviour
{
    [Header("Weapon Identity")]
    [SerializeField] private WeaponType weaponType;
    [SerializeField] private string weaponName;

    [Header("Drop Settings")]
    [Tooltip("Prefab to spawn in world when dropped")]
    [SerializeField] private GameObject droppedPrefab;

    [Header("Combat Settings")]
    [Tooltip("How much damage this weapon deals per hit")]
    [SerializeField] protected float damage = 25f;

    [Tooltip("Time between attacks/shots in seconds")]
    [SerializeField] protected float attackRate = 0.25f;

    [Header("Effects")]
    [Tooltip("The point at the very tip of the barrel — muzzle flash spawns here")]
    [SerializeField] protected Transform muzzlePoint;

    [Tooltip("Particle prefab to instantiate when the weapon fires")]
    [SerializeField] protected GameObject muzzleFlashPrefab;

    [Tooltip("Lifetime in seconds before the muzzle flash is auto-destroyed")]
    [SerializeField] protected float muzzleFlashLifetime = 0.05f;

    [Header("Fire Animation")]
    [Tooltip("Animator to trigger on fire — can be the weapon's own Animator or the character's")]
    [SerializeField] protected Animator fireAnimator;

    [Tooltip("Animator trigger parameter name to fire when shooting")]
    [SerializeField] protected string fireTriggerName = "Fire";

    [Header("Ammo Settings (Guns only)")]
    [SerializeField] protected bool usesAmmo = false;
    [SerializeField] protected AmmoType ammoType;
    [SerializeField] protected int maxMagazine = 10;

    [Header("Audio")]
    [SerializeField] protected AudioClip fireSound;
    [SerializeField] protected AudioClip reloadSound;
    protected AudioSource audioSource;
    
    protected int currentMagazine;
    protected float lastAttackTime;
    protected PlayerAmmo playerAmmo; // Reference to player's reserve ammo
    
    /// <summary>Cached root of the player — used to skip self-hits in raycasts.</summary>
    protected Transform playerRoot;

    // Public accessors
    // NOTE: Property is named 'WeaponKind' (not 'WeaponType') to avoid
    // ambiguity with the WeaponType enum that exists in the same scope.
    public WeaponType WeaponKind => weaponType;
    public string WeaponName => weaponName;
    public GameObject DroppedPrefab => droppedPrefab;
    public int CurrentMagazine => currentMagazine;
    public int MaxMagazine => maxMagazine;
    public bool UsesAmmo => usesAmmo;
    public Transform MuzzlePoint => muzzlePoint;

    protected virtual void Awake()
    {
        currentMagazine = maxMagazine;
        
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1f; // 3D sound
        }

        // Cache the player root so raycasts can reliably ignore all player colliders.
        // Works regardless of layer setup or how the weapon is parented.
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            playerRoot = playerObj.transform.root;
        else
            Debug.LogWarning("[WeaponBase] No GameObject with tag 'Player' found. " +
                             "Raycasts may hit the player's own colliders.");
    }

    /// <summary>
    /// Initializes weapon references. Called by WeaponController when equipped.
    /// </summary>
    public virtual void Initialize(PlayerAmmo ammoTracker)
    {
        playerAmmo = ammoTracker;
        if (usesAmmo)
        {
            GameEvents.RaiseMagazineChanged(WeaponKind, currentMagazine, maxMagazine);
        }
    }

    public virtual void OnEquip()
    {
        gameObject.SetActive(true);

        // Re-enable muzzle flash particle system if one lives on the muzzle point.
        // This covers the case where it was stopped on unequip.
        if (muzzlePoint != null)
        {
            var ps = muzzlePoint.GetComponentInChildren<ParticleSystem>(true);
            if (ps != null)
            {
                ps.gameObject.SetActive(true);
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }

        if (usesAmmo)
            GameEvents.RaiseMagazineChanged(WeaponKind, currentMagazine, maxMagazine);

        Debug.Log($"[Weapon] Equipped: {weaponName}");
    }

    public virtual void OnUnequip()
    {
        // Stop and hide any muzzle flash particle system so it doesn't
        // keep playing or show through geometry when no weapon is held.
        if (muzzlePoint != null)
        {
            var ps = muzzlePoint.GetComponentInChildren<ParticleSystem>(true);
            if (ps != null)
            {
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                ps.gameObject.SetActive(false);
            }
        }

        gameObject.SetActive(false);
        Debug.Log($"[Weapon] Unequipped: {weaponName}");
    }

    /// <summary>
    /// Base attack method. Returns true if the attack was executed.
    /// Subclasses MUST call base.Attack(origin) first and return false if it returns false.
    /// </summary>
    public virtual bool Attack(Transform origin) 
    { 
        if (Time.time - lastAttackTime < attackRate)
            return false;
            
        lastAttackTime = Time.time;

        // ── Audio ─────────────────────────────────────────────────────────
        if (audioSource != null && fireSound != null)
        {
            audioSource.PlayOneShot(fireSound);
        }

        // ── Muzzle Flash ──────────────────────────────────────────────────
        if (muzzleFlashPrefab != null && muzzlePoint != null)
        {
            GameObject flash = Instantiate(
                muzzleFlashPrefab,
                muzzlePoint.position,
                muzzlePoint.rotation);
            Destroy(flash, muzzleFlashLifetime);
        }

        // ── Fire Animation Trigger ────────────────────────────────────────
        if (fireAnimator != null && !string.IsNullOrEmpty(fireTriggerName))
            fireAnimator.SetTrigger(fireTriggerName);

        return true;
    }

    /// <summary>
    /// Base reload method. Fills magazine from reserve ammo.
    /// </summary>
    public virtual void Reload()
    {
        Debug.Log($"[WeaponBase.Reload] Called for {WeaponName}. usesAmmo: {usesAmmo}, playerAmmo is null: {playerAmmo == null}");
        if (!usesAmmo || playerAmmo == null) return;

        if (currentMagazine == maxMagazine)
        {
            Debug.Log($"[WeaponBase.Reload] {WeaponName} magazine is full. cur: {currentMagazine}, max: {maxMagazine}");
            GameEvents.RaiseAttackBlocked("Magazine full");
            return;
        }

        int needed = maxMagazine - currentMagazine;
        int reserve = playerAmmo.GetAmmo(ammoType);
        
        Debug.Log($"[WeaponBase.Reload] {WeaponName} needs {needed}. Reserve has {reserve}");

        if (reserve <= 0)
        {
            Debug.Log($"[WeaponBase.Reload] {WeaponName} no reserve ammo!");
            GameEvents.RaiseAttackBlocked("No reserve ammo");
            return;
        }

        GameEvents.RaiseReloadStarted(WeaponKind);

        // ── Audio ─────────────────────────────────────────────────────────
        if (audioSource != null && reloadSound != null)
        {
            audioSource.PlayOneShot(reloadSound);
        }

        int amountToLoad = Mathf.Min(needed, reserve);
        playerAmmo.UseAmmo(ammoType, amountToLoad); // Consume from reserve
        currentMagazine += amountToLoad;

        GameEvents.RaiseReloadCompleted(WeaponKind, currentMagazine);
        GameEvents.RaiseMagazineChanged(WeaponKind, currentMagazine, maxMagazine);
        Debug.Log($"[Weapon] Reloaded {weaponName}. Mag: {currentMagazine}/{maxMagazine}");
    }
}