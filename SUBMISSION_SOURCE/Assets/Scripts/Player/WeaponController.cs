using UnityEngine;

/// <summary>
/// Handles weapon interactions: attacking and reloading.
/// Aiming and Camera logic has been moved to AimCameraController and CameraSwitcher.
/// </summary>
public class WeaponController : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The camera used for hitscans")]
    [SerializeField] private Camera mainCamera;

    private PlayerAmmo playerAmmo;
    private PlayerEquipment playerEquipment;
    private EquipmentManager equipmentManager;

    private void Awake()
    {
        playerAmmo       = GetComponent<PlayerAmmo>();
        playerEquipment  = GetComponent<PlayerEquipment>();
        equipmentManager = GetComponent<EquipmentManager>();

        if (mainCamera == null) mainCamera = Camera.main;
    }

    private void OnEnable()
    {
        InputManager.OnAttackPressed += HandleAttackPressed;
        InputManager.OnSecondaryAttackPressed += HandleSecondaryAttackPressed;
        InputManager.OnReloadPressed += HandleReloadPressed;
        GameEvents.OnWeaponEquipped  += HandleWeaponEquipped;
    }

    private void OnDisable()
    {
        InputManager.OnAttackPressed -= HandleAttackPressed;
        InputManager.OnSecondaryAttackPressed -= HandleSecondaryAttackPressed;
        InputManager.OnReloadPressed -= HandleReloadPressed;
        GameEvents.OnWeaponEquipped  -= HandleWeaponEquipped;
    }

    private void Update()
    {
        // ── Automatic fire (Rifle — hold to shoot) ─────────────────────
        if (InputManager.IsAttackHeld)
        {
            WeaponBase currentWeapon = GetActiveWeapon();
            if (currentWeapon != null && currentWeapon.WeaponKind == WeaponType.Rifle)
                currentWeapon.Attack(mainCamera.transform);
        }
    }

    private WeaponBase GetActiveWeapon()
    {
        // 1st choice: EquipmentManager tracks the exact active WeaponBase instance.
        if (equipmentManager != null)
        {
            var tracked = equipmentManager.GetCurrentWeapon();
            if (tracked != null) 
            {
                // Debug.Log($"[WeaponController on {gameObject.name}] Found weapon via EquipmentManager: {tracked.WeaponName}");
                return tracked;
            }
        }

        // Fallback: scan active children
        WeaponBase[] all = GetComponentsInChildren<WeaponBase>(false);
        if (all.Length > 0)
        {
            // Debug.Log($"[WeaponController on {gameObject.name}] Found weapon via GetComponentsInChildren: {all[0].WeaponName}");
            return all[0];
        }
        
        // Debug.LogWarning($"[WeaponController on {gameObject.name}] GetActiveWeapon failed! EquipmentManager tracked nothing, and no active WeaponBase children found.");
        return null;
    }

    private void HandleWeaponEquipped(WeaponType type)
    {
        WeaponBase current = GetActiveWeapon();
        if (current != null && playerAmmo != null)
            current.Initialize(playerAmmo);
    }

    private void HandleAttackPressed()
    {
        WeaponBase current = GetActiveWeapon();
        if (current == null) return;

        // Rifle handles its own firing in Update() via IsAttackHeld.
        // Pistol and Knife trigger once on press.
        if (current.WeaponKind == WeaponType.Pistol || current.WeaponKind == WeaponType.Knife)
            current.Attack(mainCamera.transform);
    }

    private void HandleSecondaryAttackPressed()
    {
        WeaponBase current = GetActiveWeapon();
        if (current == null) return;

        // Allow Right-Click to act as an attack when holding the Knife
        if (current.WeaponKind == WeaponType.Knife)
            current.Attack(mainCamera.transform);
    }

    private void HandleReloadPressed()
    {
        WeaponBase current = GetActiveWeapon();
        // Debug.Log($"[WeaponController on {gameObject.name}] R key pressed! Active weapon found: {(current != null ? current.WeaponName : "None")}");
        if (current != null)
        {
            current.Reload();
        }
    }
}
