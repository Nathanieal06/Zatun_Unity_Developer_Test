// Assets/Scripts/Core/GameEvents.cs
using System;
using UnityEngine;

/// <summary>
/// Central event hub for the game.
/// Systems communicate through here — never directly with each other.
/// Add new events here as you build more systems.
/// </summary>
public static class GameEvents
{
    // ── Health Events ────────────────────────────────────────────────
    /// <summary>Fired when the player takes damage. float = damage amount.</summary>
    public static event Action<float> OnPlayerDamaged;

    /// <summary>Fired when the player is healed. float = heal amount.</summary>
    public static event Action<float> OnPlayerHealed;

    /// <summary>Fired when health changes. float = current, float = max.</summary>
    public static event Action<float, float> OnHealthChanged;

    /// <summary>Fired when the player dies.</summary>
    public static event Action OnPlayerDied;

    // ── Pickup Events ─────────────────────────────────────────────────
    /// <summary>Fired when a pickup enters interaction range. string = label.</summary>
    public static event Action<string> OnPickupEnterRange;

    /// <summary>Fired when no pickups are in range.</summary>
    public static event Action OnPickupExitRange;

    // ── Narrative Events ──────────────────────────────────────────────
    /// <summary>Fired for tutorial or story messages. string = message, float = duration.</summary>
    public static event Action<string, float> OnNarrativeMessage;

    // ── Equipment Events ──────────────────────────────────────────────
    /// <summary>Fired when a weapon is equipped. WeaponType = new weapon.</summary>
    public static event Action<WeaponType> OnWeaponEquipped;

    /// <summary>Fired when a weapon is dropped. WeaponType = dropped weapon.</summary>
    public static event Action<WeaponType> OnWeaponDropped;

    // ── Ammo / Reserve Events ─────────────────────────────────────────
    /// <summary>Fired when the player's reserve ammo changes. AmmoType = which pool, int = current, int = max.</summary>
    public static event Action<AmmoType, int, int> OnAmmoChanged;

    // ── Combat Events ─────────────────────────────────────────────────
    /// <summary>Fired when a gun's magazine changes. WeaponType = gun, int = current mag, int = mag size.</summary>
    public static event Action<WeaponType, int, int> OnMagazineChanged;

    /// <summary>Fired when a reload begins. WeaponType = gun being reloaded.</summary>
    public static event Action<WeaponType> OnReloadStarted;

    /// <summary>Fired when a reload finishes. WeaponType = gun, int = new mag count.</summary>
    public static event Action<WeaponType, int> OnReloadCompleted;

    /// <summary>Fired each time a hitscan shot is fired. WeaponType = gun that fired.</summary>
    public static event Action<WeaponType> OnShotFired;

    /// <summary>Fired when an attack is blocked (e.g. empty magazine). string = reason message.</summary>
    public static event Action<string> OnAttackBlocked;

    /// <summary>Fired when the player starts or stops aiming. bool = isAiming.</summary>
    public static event Action<bool> OnAimStateChanged;

    /// <summary>Fired when any zombie dies. GameObject = the zombie that died.</summary>
    public static event Action<GameObject> OnZombieDied;

    /// <summary>Fired when the player achieves victory.</summary>
    public static event Action OnGameWin;

    // ── System Events ─────────────────────────────────────────────────
    /// <summary>Fired when the game is paused or resumed. bool = isPaused.</summary>
    public static event Action<bool> OnPauseToggled;

    // ── Raise Methods (called by the systems that own the data) ───────
    public static void RaisePlayerDamaged(float amount)   => OnPlayerDamaged?.Invoke(amount);
    public static void RaisePlayerHealed(float amount)    => OnPlayerHealed?.Invoke(amount);
    public static void RaiseHealthChanged(float cur, float max) => OnHealthChanged?.Invoke(cur, max);
    public static void RaisePlayerDied()                  => OnPlayerDied?.Invoke();
    public static void RaisePickupEnterRange(string label)=> OnPickupEnterRange?.Invoke(label);
    public static void RaisePickupExitRange()             => OnPickupExitRange?.Invoke();
    public static void RaiseWeaponEquipped(WeaponType t)  => OnWeaponEquipped?.Invoke(t);
    public static void RaiseWeaponDropped(WeaponType t)   => OnWeaponDropped?.Invoke(t);
    public static void RaiseAmmoChanged(AmmoType t, int cur, int max)            => OnAmmoChanged?.Invoke(t, cur, max);
    public static void RaiseMagazineChanged(WeaponType t, int cur, int mag)       => OnMagazineChanged?.Invoke(t, cur, mag);
    public static void RaiseReloadStarted(WeaponType t)                           => OnReloadStarted?.Invoke(t);
    public static void RaiseReloadCompleted(WeaponType t, int newMag)             => OnReloadCompleted?.Invoke(t, newMag);
    public static void RaiseShotFired(WeaponType t)                               => OnShotFired?.Invoke(t);
    public static void RaiseAttackBlocked(string reason)                          => OnAttackBlocked?.Invoke(reason);
    public static void RaiseAimStateChanged(bool isAiming)                        => OnAimStateChanged?.Invoke(isAiming);
    public static void RaiseZombieDied(GameObject zombie)                        => OnZombieDied?.Invoke(zombie);
    public static void RaiseGameWin()                                            => OnGameWin?.Invoke();
    public static void RaiseNarrativeMessage(string msg, float dur)               => OnNarrativeMessage?.Invoke(msg, dur);
    public static void RaisePauseToggled(bool isPaused)                           => OnPauseToggled?.Invoke(isPaused);
}