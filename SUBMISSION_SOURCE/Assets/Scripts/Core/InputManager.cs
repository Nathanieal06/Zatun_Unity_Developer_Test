// Assets/Scripts/Core/InputManager.cs
using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Centralized input handler using Unity's New Input System.
/// All other scripts subscribe to these events — never read input directly.
///
/// EXTENDED (Day 3): Added Aim, Attack, and Reload events for the
/// WeaponController system. The separation keeps WeaponController fully
/// decoupled from the input binding layer — swap input sources without
/// touching weapon logic.
/// </summary>
public class InputManager : MonoBehaviour
{
    public enum AimModeType { Hold, Toggle }

    [Header("Settings")]
    [Tooltip("Hold: Aiming requires holding the right mouse button. Toggle: Aiming toggles on/off with each click.")]
    public AimModeType aimMode = AimModeType.Hold;

    // ── Existing Events ───────────────────────────────────────────────────
    public static event Action OnPickupPressed;
    public static event Action OnDropPressed;
    public static event Action<int> OnWeaponSwitch;
    public static event Action OnPausePressed;

    // DEBUG ONLY
    public static event Action OnDebugDamage;
    public static event Action OnDebugHeal;

    // ── NEW: Aim Events ───────────────────────────────────────────────────
    /// <summary>Fired once when Right-Click is pressed (aim starts).</summary>
    public static event Action OnAimStarted;

    /// <summary>Fired once when Right-Click is released (aim ends).</summary>
    public static event Action OnAimCanceled;

    // ── NEW: Attack Events ────────────────────────────────────────────────
    /// <summary>
    /// Fired once when Left-Click is pressed.
    /// Used by: Knife (trigger melee) and Pistol (fire one shot).
    /// </summary>
    public static event Action OnAttackPressed;

    /// <summary>Fired once when Left-Click is released.</summary>
    public static event Action OnAttackReleased;

    /// <summary>Fired when Right-Click is pressed but the weapon cannot aim (e.g., Knife).</summary>
    public static event Action OnSecondaryAttackPressed;

    // ── NEW: Reload Event ─────────────────────────────────────────────────
    /// <summary>Fired once when R is pressed. WeaponController routes this to the active gun.</summary>
    public static event Action OnReloadPressed;

    // ── Internal State ────────────────────────────────────────────────────

    /// <summary>
    /// Readable by WeaponController in Update() to drive auto-fire without
    /// needing a per-frame event. True while Left-Click is held.
    /// </summary>
    public static bool IsAttackHeld { get; private set; }

    /// <summary>True while Right-Click is held (used by WeaponController for continuous aim).</summary>
    public static bool IsAimHeld { get; private set; }

    /// <summary>True if the currently equipped weapon allows aiming.</summary>
    public static bool CanAim { get; private set; } = false;

    public static void SetCanAim(bool canAim)
    {
        CanAim = canAim;
        // Force cancel aiming if the player switches to a weapon that cannot aim
        if (!CanAim && IsAimHeld)
        {
            IsAimHeld = false;
            OnAimCanceled?.Invoke();
        }
    }

    // ── Private ───────────────────────────────────────────────────────────
    private PlayerControls inputActions;

    private void Awake()
    {
        inputActions = new PlayerControls();
    }

    private void OnEnable()
    {
        inputActions.Player.Enable();

        // ── Existing Wiring ────────────────────────────────────────────
        inputActions.Player.Pickup.performed += _ => { if (!UIManager.IsPaused) OnPickupPressed?.Invoke(); };
        inputActions.Player.Drop.performed   += _ => { if (!UIManager.IsPaused) OnDropPressed?.Invoke(); };

        inputActions.Player.WeaponSlot1.performed += _ => { if (!UIManager.IsPaused) OnWeaponSwitch?.Invoke(0); };
        inputActions.Player.WeaponSlot2.performed += _ => { if (!UIManager.IsPaused) OnWeaponSwitch?.Invoke(1); };
        inputActions.Player.WeaponSlot3.performed += _ => { if (!UIManager.IsPaused) OnWeaponSwitch?.Invoke(2); };
        inputActions.Player.WeaponSlot4.performed += _ => { if (!UIManager.IsPaused) OnWeaponSwitch?.Invoke(3); };

        inputActions.Player.DebugDamage.performed += _ => { if (!UIManager.IsPaused) OnDebugDamage?.Invoke(); };
        inputActions.Player.DebugHeal.performed   += _ => { if (!UIManager.IsPaused) OnDebugHeal?.Invoke(); };

        // ── NEW: Aim Wiring ────────────────────────────────────────────
        inputActions.Player.Aim.performed += _ =>
        {
            if (UIManager.IsPaused) return;

            // If we can't aim (Knife/None), just fire the secondary attack and STOP.
            // This prevents the camera from "diverting" or trying to enter aim mode.
            if (!CanAim) 
            {
                IsAimHeld = false; // Force safety
                OnSecondaryAttackPressed?.Invoke();
                return;
            }
            
            if (aimMode == AimModeType.Hold)
            {
                IsAimHeld = true;
                OnAimStarted?.Invoke();
            }
            else // Toggle Mode
            {
                IsAimHeld = !IsAimHeld;
                if (IsAimHeld)
                    OnAimStarted?.Invoke();
                else
                    OnAimCanceled?.Invoke();
            }
        };
        inputActions.Player.Aim.canceled += _ =>
        {
            // Always allow cancel to clean up state
            if (aimMode == AimModeType.Hold || !CanAim)
            {
                IsAimHeld = false;
                if (CanAim) OnAimCanceled?.Invoke();
            }
        };

        // ── NEW: Attack Wiring ─────────────────────────────────────────
        inputActions.Player.Attack.performed += _ =>
        {
            if (UIManager.IsPaused) return;
            IsAttackHeld = true;
            OnAttackPressed?.Invoke();
        };
        inputActions.Player.Attack.canceled += _ =>
        {
            if (UIManager.IsPaused) return;
            IsAttackHeld = false;
            OnAttackReleased?.Invoke();
        };

        // ── NEW: Reload Wiring ─────────────────────────────────────────
        inputActions.Player.Reload.performed += _ => { if (!UIManager.IsPaused) OnReloadPressed?.Invoke(); };

        // ── NEW: Pause Wiring ──────────────────────────────────────────
        inputActions.Player.Pause.performed += _ => OnPausePressed?.Invoke(); // Pause ALWAYS works
    }

    private void OnDisable()
    {
        // Always disable and dispose to prevent memory leaks
        inputActions.Player.Disable();
        inputActions.Dispose();

        // Reset held-state flags so nothing gets stuck when re-enabling
        IsAttackHeld = false;
        IsAimHeld    = false;
    }
}