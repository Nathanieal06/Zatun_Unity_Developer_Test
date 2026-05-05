// Assets/Scripts/Player/PlayerEquipment.cs
using UnityEngine;

/// <summary>
/// Fully self-contained player inventory and equipment manager.
///
/// Responsibilities:
///   - Track which weapons the player has unlocked (Knife, Pistol, Rifle).
///   - Toggle the 3D weapon hand-models on/off via SetActive.
///   - Listen to number keys (1-4) for switching and G for dropping.
///   - Expose UnlockWeapon() so WeaponPickup can grant a weapon on pickup.
///   - Drive an Animator "EquipmentID" integer parameter for weapon stances.
///
/// Setup (in Inspector):
///   1. Attach this to the Player GameObject (same object as PickupInteractor).
///   2. Assign the three weapon model GameObjects under "Weapon Models".
///   3. (Optional) Assign droppedPrefabs if you want world objects to spawn on drop.
///   4. (Optional) Assign Player Animator to drive weapon stance animations.
/// </summary>
public class PlayerEquipment : MonoBehaviour
{
    // ── Inspector ────────────────────────────────────────────────────────

    [Header("Weapon Models")]
    [Tooltip("3D model for the Knife shown in the player's hand")]
    [SerializeField] private GameObject knifeModel;

    [Tooltip("3D model for the Pistol shown in the player's hand")]
    [SerializeField] private GameObject pistolModel;

    [Tooltip("3D model for the Assault Rifle shown in the player's hand")]
    [SerializeField] private GameObject rifleModel;

    [Header("Drop Settings")]
    [Tooltip("Prefab to spawn in world when the Knife is dropped")]
    [SerializeField] private GameObject knifeDropPrefab;

    [Tooltip("Prefab to spawn in world when the Pistol is dropped")]
    [SerializeField] private GameObject pistolDropPrefab;

    [Tooltip("Prefab to spawn in world when the Rifle is dropped")]
    [SerializeField] private GameObject rifleDropPrefab;

    [Tooltip("How far in front of the player the weapon is spawned")]
    [SerializeField] private float dropDistance = 1.5f;

    // [Tooltip("Upward launch force applied to the dropped weapon Rigidbody")]
    // [SerializeField] private float dropUpwardForce = 2f;

    [Header("Animation")]
    [Tooltip("Animator on the player — drives 'EquipmentID' integer parameter for weapon stances")]
    [SerializeField] private Animator playerAnimator;

    // ── Internal State ───────────────────────────────────────────────────

    private bool hasKnife;
    private bool hasPistol;
    private bool hasRifle;

    // Stores the world rotation the weapon had when it was picked up
    private Quaternion knifePickupRotation  = Quaternion.identity;
    private Quaternion pistolPickupRotation = Quaternion.identity;
    private Quaternion riflePickupRotation  = Quaternion.identity;

    private WeaponType currentWeaponType = WeaponType.None;

    // ── Unity Lifecycle ──────────────────────────────────────────────────

    private void Awake()
    {
        // Auto-find the Animator if not assigned in Inspector
        if (playerAnimator == null)
            playerAnimator = GetComponentInChildren<Animator>();

        if (playerAnimator == null)
        {
            // Debug.LogWarning("[PlayerEquipment] No Animator found — EquipmentID will not update. Assign it in the Inspector or ensure an Animator exists in the hierarchy.");
        }

        // Ensure all hand models start hidden
        SetModelActive(knifeModel,  false);
        SetModelActive(pistolModel, false);
        SetModelActive(rifleModel,  false);
    }

    private void OnEnable()
    {
        InputManager.OnWeaponSwitch += HandleWeaponSwitch;
        InputManager.OnDropPressed  += HandleDrop;
    }

    private void OnDisable()
    {
        InputManager.OnWeaponSwitch -= HandleWeaponSwitch;
        InputManager.OnDropPressed  -= HandleDrop;
    }

    // ── Public API ───────────────────────────────────────────────────────

    /// <summary>
    /// Called by WeaponPickup when the player picks up a weapon.
    /// Stores the pickup's original rotation for use when dropping.
    /// Always switches to the newly picked up weapon immediately.
    /// </summary>
    public void UnlockWeapon(WeaponType type, Quaternion pickupRotation = default)
    {
        switch (type)
        {
            case WeaponType.Knife:
                if (hasKnife) { /* Debug.Log("[PlayerEquipment] Knife already owned."); */ return; }
                hasKnife = true;
                knifePickupRotation = pickupRotation;
                break;

            case WeaponType.Pistol:
                if (hasPistol) { /* Debug.Log("[PlayerEquipment] Pistol already owned."); */ return; }
                hasPistol = true;
                pistolPickupRotation = pickupRotation;
                break;

            case WeaponType.Rifle:
                if (hasRifle) { /* Debug.Log("[PlayerEquipment] Rifle already owned."); */ return; }
                hasRifle = true;
                riflePickupRotation = pickupRotation;
                break;

            default:
                // Debug.LogWarning($"[PlayerEquipment] Unknown weapon type: {type}");
                return;
        }

        // Debug.Log($"[PlayerEquipment] Unlocked: {type}");

        // Always switch to the newly picked up weapon immediately
        EquipWeapon(type);
    }

    public bool HasWeapon(WeaponType type) => type switch
    {
        WeaponType.Knife  => hasKnife,
        WeaponType.Pistol => hasPistol,
        WeaponType.Rifle  => hasRifle,
        _                 => false
    };

    public WeaponType CurrentWeapon => currentWeaponType;

    // ── Input Handlers ───────────────────────────────────────────────────

    /// <summary>
    /// Number key mapping:
    ///   1 → None (unarmed)
    ///   2 → Knife
    ///   3 → Pistol
    ///   4 → Rifle
    /// </summary>
    private void HandleWeaponSwitch(int index)
    {
        WeaponType requested = index switch
        {
            0 => WeaponType.None,
            1 => WeaponType.Knife,
            2 => WeaponType.Pistol,
            3 => WeaponType.Rifle,
            _ => WeaponType.None
        };

        if (requested != WeaponType.None && !HasWeapon(requested))
        {
            // Debug.Log($"[PlayerEquipment] {requested} not in inventory.");
            return;
        }

        EquipWeapon(requested);
    }

    /// <summary>
    /// G key — drops current weapon, spawns it as a world pickup, clears the slot.
    /// </summary>
    private void HandleDrop()
    {
        // Don't process input while the game is paused
        if (Time.timeScale == 0f) return;

        if (currentWeaponType == WeaponType.None)
        {
            // Debug.Log("[PlayerEquipment] Nothing to drop.");
            return;
        }

        // Cache before clearing so model hide targets the correct object
        WeaponType dropping = currentWeaponType;
        GameObject modelToHide = GetModelForType(dropping);

        // Clear inventory slot first
        switch (dropping)
        {
            case WeaponType.Knife:  hasKnife  = false; break;
            case WeaponType.Pistol: hasPistol = false; break;
            case WeaponType.Rifle:  hasRifle  = false; break;
        }
        currentWeaponType = WeaponType.None;

        // Hide the hand model
        if (modelToHide != null)
            modelToHide.SetActive(false);
        else
        {
            // Debug.LogWarning($"[PlayerEquipment] Hand model for {dropping} is NULL — assign it in the Inspector under 'Weapon Models'.");
        }

        // Update animator to unarmed state
        SetAnimatorWeapon(WeaponType.None);

        // Sync EquipmentManager so the WeaponBase instance is also deactivated.
        // Without this, WeaponController.GetActiveWeapon() would still find the
        // old weapon and allow shooting after the player has dropped it.
        var eqManager = GetComponent<EquipmentManager>();
        if (eqManager != null)
            eqManager.EquipByTypePublic(WeaponType.None);

        InputManager.SetCanAim(false);
        GameEvents.RaiseWeaponEquipped(WeaponType.None);

        SpawnDroppedWeapon(dropping);
        GameEvents.RaiseWeaponDropped(dropping);
        // Debug.Log($"[PlayerEquipment] Dropped: {dropping}");
    }

    // ── Private Helpers ──────────────────────────────────────────────────
    private void EquipWeapon(WeaponType type)
    {
        if (type == currentWeaponType) return;

        // Hide the old model
        SetModelActive(GetModelForType(currentWeaponType), false);

        currentWeaponType = type;

        // Show the new model
        SetModelActive(GetModelForType(currentWeaponType), true);

        // Drive animator stance parameter
        SetAnimatorWeapon(type);

        // Sync with EquipmentManager (the new combat system) if it exists
        var eqManager = GetComponent<EquipmentManager>();
        if (eqManager != null)
        {
            eqManager.EquipByTypePublic(type);
        }

        InputManager.SetCanAim(type == WeaponType.Pistol || type == WeaponType.Rifle);
        GameEvents.RaiseWeaponEquipped(type);

        // Debug.Log($"[PlayerEquipment] Equipped: {(type == WeaponType.None ? "None" : type.ToString())}");
    }

    /// <summary>
    /// Pushes the current weapon type to the Animator as an integer.
    /// None=0, Knife=1, Pistol=2, Rifle=3 (matches WeaponType enum values).
    /// Create an 'EquipmentID' Integer parameter in your Animator to use this.
    /// </summary>
    private void SetAnimatorWeapon(WeaponType type)
    {
        if (playerAnimator != null)
            playerAnimator.SetInteger("EquipmentID", (int)type);
    }

    private void SpawnDroppedWeapon(WeaponType type)
    {
        GameObject prefab = type switch
        {
            WeaponType.Knife  => knifeDropPrefab,
            WeaponType.Pistol => pistolDropPrefab,
            WeaponType.Rifle  => rifleDropPrefab,
            _                 => null
        };

        if (prefab == null)
        {
            // Debug.LogWarning($"[PlayerEquipment] No drop prefab assigned for {type}.");
            return;
        }

        // Restore original rotation and place flat on the ground (y = 0)
        Quaternion originalRotation = type switch
        {
            WeaponType.Knife  => knifePickupRotation,
            WeaponType.Pistol => pistolPickupRotation,
            WeaponType.Rifle  => riflePickupRotation,
            _                 => Quaternion.identity
        };

        Vector3 dropPos = new Vector3(
            transform.position.x + transform.forward.x * dropDistance,
            0f,
            transform.position.z + transform.forward.z * dropDistance
        );

        var dropped = Instantiate(prefab, dropPos, originalRotation);

        // Ensure a Collider exists BEFORE adding WeaponPickup.
        // BasePickup.Awake() calls GetComponent<Collider>() — it must be
        // present already or the isTrigger assignment will fail.
        if (dropped.GetComponent<Collider>() == null)
            dropped.AddComponent<SphereCollider>();

        var pickup = dropped.GetComponent<WeaponPickup>()
                  ?? dropped.AddComponent<WeaponPickup>();

        if (pickup != null)
            pickup.weaponType = type;
        else
        {
            // Debug.LogWarning($"[PlayerEquipment] Failed to get WeaponPickup on dropped {type} — re-pickup will not work.");
        }
    }

    private GameObject GetModelForType(WeaponType type) => type switch
    {
        WeaponType.Knife  => knifeModel,
        WeaponType.Pistol => pistolModel,
        WeaponType.Rifle  => rifleModel,
        _                 => null
    };

    private static void SetModelActive(GameObject model, bool active)
    {
        if (model != null)
            model.SetActive(active);
    }
}
