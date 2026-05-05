using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ZombieCombat : MonoBehaviour
{
    [HideInInspector] public ZombieController controller;
    private Animator anim;
    private Transform player;
    private PlayerHealth playerHealth;
    
    [HideInInspector] public float attackTimer;
    [HideInInspector] public AttackData currentAttack;

    public void Initialize(ZombieController controller, Animator anim, Transform player, PlayerHealth playerHealth)
    {
        this.controller = controller;
        this.anim = anim;
        this.player = player;
        this.playerHealth = playerHealth;
    }

    void Update()
    {
        if (attackTimer > 0) attackTimer -= Time.deltaTime;
        else attackTimer = 0;
    }

    public void HandleAttack()
    {
        if (player == null) return;

        Vector3 direction = (player.position - transform.position).normalized;
        direction.y = 0;
        if (direction != Vector3.zero)
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), Time.deltaTime * 12f);

        float distanceToPlayer = controller.zombieAI.Get2DDistanceToPlayer();

        bool inRange = false;
        if (controller.attacks != null)
            foreach (var attack in controller.attacks)
                if (distanceToPlayer <= attack.range) { inRange = true; break; }

        bool isPlayingAttackAnim = false;
        if (anim != null)
        {
            AnimatorStateInfo stateInfo = anim.GetCurrentAnimatorStateInfo(0);
            // Check for tags, but also check if the state name itself contains "Attack"
            bool isAttackState = stateInfo.IsTag("Attack") || stateInfo.IsTag("Attack1") || stateInfo.IsTag("Attack2") || stateInfo.IsName("Attack");

            if (isAttackState)
            {
                if (stateInfo.normalizedTime >= 0.95f && !anim.IsInTransition(0))
                {
                    anim.CrossFadeInFixedTime("Locomotion", 0.25f);
                    isPlayingAttackAnim = false;
                }
                else isPlayingAttackAnim = true;
            }
            else isPlayingAttackAnim = anim.IsInTransition(0);
        }

        if (currentAttack != null && attackTimer > currentAttack.cooldown - 0.2f && currentAttack.cooldown > 0f) 
        {
            isPlayingAttackAnim = true;
        }

        if (!inRange && !isPlayingAttackAnim)
        {
            if (anim != null)
            {
                anim.applyRootMotion = false;
                anim.CrossFadeInFixedTime("Locomotion", 0.25f);
            }
            if (controller.agent.isOnNavMesh) controller.agent.isStopped = false;
            controller.currentState = ZombieController.ZombieState.Aggro;
            return;
        }

        if (controller.agent.isOnNavMesh)
        {
            controller.agent.isStopped = true;
            controller.agent.velocity = Vector3.zero;
        }

        if (attackTimer <= 0f && !isPlayingAttackAnim && inRange) PerformRandomAttack();
    }

    public void PerformRandomAttack()
    {
        if (controller.attacks == null || controller.attacks.Length == 0) return;

        float distanceToPlayer = controller.zombieAI.Get2DDistanceToPlayer();
        
        // If player HP is critically low (< 30), always use Attack3 if it's in range
        if (playerHealth != null && playerHealth.GetCurrentHealth() < 30f)
        {
            AttackData attack3 = null;
            foreach (var attack in controller.attacks)
            {
                if (attack.animTrigger == "Attack3" && distanceToPlayer <= attack.range)
                {
                    attack3 = attack;
                    break;
                }
            }
            if (attack3 != null)
            {
                currentAttack = attack3;
                if (anim != null)
                {
                    foreach (var atk in controller.attacks) anim.ResetTrigger(atk.animTrigger);
                    anim.SetTrigger(currentAttack.animTrigger);
                }
                attackTimer = currentAttack.cooldown;
                if (controller.useAutoHitbox && !currentAttack.isRanged)
                    StartCoroutine(AutoHitboxRoutine(currentAttack.hitboxDelay, currentAttack.hitboxDuration));
                return;
            }
        }

        bool playerLowHealth = playerHealth != null && playerHealth.GetCurrentHealth() < 30f;

        List<AttackData> validAttacks = new List<AttackData>();
        foreach (var attack in controller.attacks)
        {
            if (distanceToPlayer > attack.range) continue;

            // Attack3 is reserved for when the player is below 30 HP
            if (attack.animTrigger == "Attack3" && !playerLowHealth) continue;

            validAttacks.Add(attack);
        }

        if (validAttacks.Count == 0) return;

        int randomIndex = Random.Range(0, validAttacks.Count);
        currentAttack = validAttacks[randomIndex];

        if (anim != null)
        {
            foreach (var atk in controller.attacks) anim.ResetTrigger(atk.animTrigger);
            anim.SetTrigger(currentAttack.animTrigger);
        }

        attackTimer = currentAttack.cooldown;

        if (controller.useAutoHitbox && !currentAttack.isRanged)
        {
            // Start the damage routine regardless of whether a physical hitbox exists.
            // If no hitbox exists, EnableHitbox() will use its robust fallback logic.
            StartCoroutine(AutoHitboxRoutine(currentAttack.hitboxDelay, currentAttack.hitboxDuration));
        }
        else
        {
            Debug.Log($"[ZombieCombat] {gameObject.name} performed attack {currentAttack.animTrigger} but did not start AutoHitbox (useAutoHitbox: {controller.useAutoHitbox}, isRanged: {currentAttack.isRanged})");
        }
    }

    private IEnumerator AutoHitboxRoutine(float delay, float duration)
    {
        yield return new WaitForSeconds(delay);
        
        bool hasHitThisSwing = false;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            // If we've already hit the player in this specific animation window,
            // we don't apply damage again.
            if (!hasHitThisSwing)
            {
                if (EnableHitboxWithReturn())
                {
                    hasHitThisSwing = true;
                }
            }
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        DisableHitbox();
    }

    public void EnableHitbox() => EnableHitboxWithReturn();

    public bool EnableHitboxWithReturn()
    {
        if (player == null || currentAttack == null) return false;

        bool hitSuccess = false;
        float damageToDeal = currentAttack.damage;

        if (controller.attackHitbox != null && !currentAttack.isRanged)
        {
            controller.attackHitbox.enabled = true;
            
            // Check for hits immediately via OverlapSphere
            Collider[] hits = Physics.OverlapSphere(controller.attackHitbox.bounds.center, controller.attackHitbox.bounds.extents.magnitude * 1.8f);
            foreach (Collider hit in hits)
            {
                PlayerHealth ph = hit.GetComponentInParent<PlayerHealth>() ?? hit.GetComponentInChildren<PlayerHealth>() ?? hit.GetComponent<PlayerHealth>();
                if (ph != null)
                {
                    Debug.Log($"[ZombieCombat] Physical Hit! Dealt {damageToDeal} damage to {ph.gameObject.name}.");
                    ph.TakeDamage(damageToDeal);
                    hitSuccess = true;
                    break;
                }
            }
        }
        
        // Robust fallback if no physical hitbox hit occurred
        if (!hitSuccess && !currentAttack.isRanged)
        {
            float dist = controller.zombieAI.Get2DDistanceToPlayer();
            float maxRange = (currentAttack != null ? currentAttack.range : 1.5f) + 1.5f;

            if (dist <= maxRange)
            {
                Vector3 dirToPlayer = (player.position - transform.position).normalized;
                float angle = Vector3.Angle(transform.forward, dirToPlayer);
                
                // If extremely close (hugging), allow hit from any angle. 
                // Otherwise allow a wide 160-degree cone.
                if (dist < 1.0f || angle < 80f) 
                {
                    if (playerHealth != null)
                    {
                        Debug.Log($"[ZombieCombat] Fallback Hit! Dist: {dist:F2}, Angle: {angle:F2}. Dealt {damageToDeal} damage.");
                        playerHealth.TakeDamage(damageToDeal);
                        hitSuccess = true;
                    }
                }
                else
                {
                    // Log why it missed for debugging
                    // Debug.Log($"[ZombieCombat] Attack missed due to angle: {angle:F2} (Max: 80)");
                }
            }
        }

        return hitSuccess;
    }

    public void DisableHitbox()
    {
        if (controller.attackHitbox != null) controller.attackHitbox.enabled = false;
    }

    public void SpawnAcidProjectile()
    {
        if (!currentAttack.isRanged || controller.acidProjectilePrefab == null || controller.acidSpawnPoint == null || player == null) return;

        GameObject projectile = Instantiate(controller.acidProjectilePrefab, controller.acidSpawnPoint.position, controller.acidSpawnPoint.rotation);
        Rigidbody rb = projectile.GetComponent<Rigidbody>();
        
        if (rb != null)
        {
            Vector3 direction = (player.position - controller.acidSpawnPoint.position).normalized;
            direction.y += 0.1f; 
            rb.AddForce(direction * controller.projectileForce, ForceMode.Impulse);
        }
    }

    public float GetCurrentDamage()
    {
        return currentAttack.damage;
    }
}
