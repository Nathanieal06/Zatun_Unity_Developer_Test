// Assets/Scripts/Player/PlayerHealth.cs
using System.Collections;
using UnityEngine;

/// <summary>
/// Manages all player health state.
/// Broadcasts events for UI and feedback — does NOT touch them directly.
/// Designed to be called by: enemies (future), traps, pickups, debug keys.
/// </summary>
public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float currentHealth;

    [Header("Regen Settings")]
    [Tooltip("Seconds after last damage before regen starts")]
    [SerializeField] private float regenDelay = 4f;
    [Tooltip("Health restored per second during regen")]
    [SerializeField] private float regenRate = 5f;

    [Header("Debug Settings")]
    [SerializeField] private float debugDamageAmount = 20f;
    [SerializeField] private float debugHealAmount = 25f;

    [Header("Hit Animation")]
    [Tooltip("Animator on the player model. Auto-found if left empty.")]
    [SerializeField] private Animator playerAnimator;
    [Tooltip("Exact name of the trigger parameter in the Animator for the hit reaction.")]
    [SerializeField] private string hitTriggerName = "Hit";
    [Tooltip("State to CrossFade back to after the Hit animation finishes.")]
    [SerializeField] private string returnStateName = "Normal Locomotion";

    // Internal state
    private bool isDead = false;
    private Coroutine regenCoroutine;
    private Coroutine hitReactionCoroutine;

    // ── Unity Lifecycle ───────────────────────────────────────────────

    private void Awake()
    {
        currentHealth = maxHealth;

        // Auto-find the animator on this object or its children if not assigned
        if (playerAnimator == null)
            playerAnimator = GetComponentInChildren<Animator>();
    }

    private void OnEnable()
    {
        // Subscribe to input events
        InputManager.OnDebugDamage += HandleDebugDamage;
        InputManager.OnDebugHeal += HandleDebugHeal;
    }

    private void OnDisable()
    {
        // ALWAYS unsubscribe — prevents memory leaks and ghost callbacks
        InputManager.OnDebugDamage -= HandleDebugDamage;
        InputManager.OnDebugHeal -= HandleDebugHeal;
    }

    private void Start()
    {
        // Broadcast initial values so UI initializes correctly
        GameEvents.RaiseHealthChanged(currentHealth, maxHealth);
    }

    // ── Public API (called by enemies, pickups, etc.) ─────────────────

    /// <summary>
    /// Apply damage to the player. Called by enemies, traps, hazards.
    /// </summary>
    public void TakeDamage(float amount)
    {
        if (isDead) return;

        currentHealth = Mathf.Clamp(currentHealth - amount, 0f, maxHealth);

        Debug.Log($"[PlayerHealth] Damage Received: {amount}. Current Health: {currentHealth}/{maxHealth}");

        // Broadcast damage event (triggers feedback systems)
        GameEvents.RaisePlayerDamaged(amount);
        GameEvents.RaiseHealthChanged(currentHealth, maxHealth);

        // Restart regen timer — damage resets the countdown
        if (regenCoroutine != null)
            StopCoroutine(regenCoroutine);

        if (currentHealth <= 0f)
        {
            Die();
        }
        else
        {
            // Play hit reaction animation then return to locomotion
            if (playerAnimator != null && !string.IsNullOrEmpty(hitTriggerName))
            {
                playerAnimator.ResetTrigger(hitTriggerName);
                playerAnimator.SetTrigger(hitTriggerName);

                if (hitReactionCoroutine != null) StopCoroutine(hitReactionCoroutine);
                hitReactionCoroutine = StartCoroutine(HitReactionRoutine());
            }
            regenCoroutine = StartCoroutine(RegenAfterDelay());
        }
    }

    /// <summary>
    /// Restore health to the player. Called by health pickups, medkits.
    /// </summary>
    public void Heal(float amount)
    {
        if (isDead) return;

        currentHealth = Mathf.Clamp(currentHealth + amount, 0f, maxHealth);

        // Debug.Log($"[PlayerHealth] Healed {amount}. Current HP: {currentHealth}/{maxHealth}");

        GameEvents.RaisePlayerHealed(amount);
        GameEvents.RaiseHealthChanged(currentHealth, maxHealth);
    }

    // ── Getters (for systems that need to read health) ────────────────

    public float GetCurrentHealth() => currentHealth;
    public float GetMaxHealth() => maxHealth;
    public bool IsDead() => isDead;

    // ── Private Logic ─────────────────────────────────────────────────

    private void Die()
    {
        if (isDead) return;
        isDead = true;

        // Debug.Log("[PlayerHealth] Player has died.");

        // Stop any ongoing regen
        if (regenCoroutine != null)
            StopCoroutine(regenCoroutine);

        // Disable player controller so they can't move after death
        // We get it here to avoid a hard reference in the field
        var controller = GetComponent<CharacterController>();
        if (controller != null) controller.enabled = false;

        GameEvents.RaisePlayerDied();
    }

    private IEnumerator RegenAfterDelay()
    {
        // Wait for the regen delay
        yield return new WaitForSeconds(regenDelay);

        // Regen tick loop
        while (currentHealth < maxHealth)
        {
            currentHealth = Mathf.Clamp(currentHealth + regenRate * Time.deltaTime, 0f, maxHealth);
            GameEvents.RaiseHealthChanged(currentHealth, maxHealth);
            yield return null; // wait one frame
        }

        regenCoroutine = null;
    }

    private IEnumerator HitReactionRoutine()
    {
        if (playerAnimator == null) yield break;

        // Wait one frame for the Animator to enter the Hit state
        yield return null;

        // Read the length of the Hit clip directly from the Animator
        float hitClipLength = 0.5f; // fallback
        foreach (AnimationClip clip in playerAnimator.runtimeAnimatorController.animationClips)
        {
            if (clip.name == hitTriggerName)
            {
                hitClipLength = clip.length;
                break;
            }
        }

        // Wait for the hit animation to finish
        yield return new WaitForSeconds(hitClipLength);

        // CrossFade smoothly back to the previous locomotion/idle state
        if (!isDead && !string.IsNullOrEmpty(returnStateName))
        {
            playerAnimator.CrossFadeInFixedTime(returnStateName, 0.2f);
        }

        hitReactionCoroutine = null;
    }

    // ── Debug Handlers ────────────────────────────────────────────────

    private void HandleDebugDamage() => TakeDamage(debugDamageAmount);
    private void HandleDebugHeal() => Heal(debugHealAmount);
}