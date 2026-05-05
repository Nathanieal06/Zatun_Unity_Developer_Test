using UnityEngine;
using System.Collections;
using UnityEngine.AI;

public class ZombieHealth : MonoBehaviour
{
    private ZombieController controller;
    private Animator anim;
    private NavMeshAgent agent;

    [HideInInspector] public float currentHealth;
    [HideInInspector] public float maxHealth = 100f;

    private void Awake()
    {
        // Safety: Auto-find references if Initialize wasn't called
        if (controller == null) controller = GetComponent<ZombieController>();
        if (anim == null) anim = GetComponentInChildren<Animator>();
        if (agent == null) agent = GetComponent<NavMeshAgent>();
        
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 1f;
            audioSource.playOnAwake = false;
        }

        if (currentHealth <= 0) currentHealth = maxHealth;
    }

    public void Initialize(ZombieController controller, Animator anim, NavMeshAgent agent, float maxHealth)
    {
        this.controller = controller;
        this.anim = anim;
        this.agent = agent;
        this.maxHealth = maxHealth;
        this.currentHealth = maxHealth;
    }

    [Header("Hit Reaction")]
    [Tooltip("How long (seconds) the NavMeshAgent stops when the zombie takes damage.")]
    [SerializeField] private float hitStunDuration = 0.5f;

    [Header("Audio")]
    [SerializeField] private AudioClip hurtSound;
    [SerializeField] private AudioClip deathSound;
    private AudioSource audioSource;

    [Header("Loot Drop")]
    [SerializeField] private GameObject healthPickupPrefab;
    [SerializeField] private GameObject pistolAmmoPrefab;
    [SerializeField] private GameObject rifleAmmoPrefab;
    [Range(0, 1)] [SerializeField] private float dropChance = 0.4f;

    private Coroutine stunCoroutine;

    public void TakeDamage(float damage)
    {
        currentHealth -= damage;
        Debug.Log($"[ZombieHealth] {gameObject.name} took {damage} damage! Remaining Health: {currentHealth}/{maxHealth}");
        
        // Play hurt sound
        if (audioSource != null && hurtSound != null)
        {
            audioSource.PlayOneShot(hurtSound);
        }

        // Ensure the zombie turns toward the player when hit
        if (controller != null && controller.currentState != ZombieController.ZombieState.Dead)
        {
            controller.ForceAggro();
        }

        if (currentHealth <= 0 && (controller == null || controller.currentState != ZombieController.ZombieState.Dead))
        {
            StartCoroutine(Die());
        }
        else if (controller != null && controller.currentState != ZombieController.ZombieState.Dead)
        {
            if (anim != null)
            {
                anim.ResetTrigger("TakeDamage");
                anim.SetTrigger("TakeDamage");
                anim.CrossFadeInFixedTime("TakeDamage", 0.1f);
                
                if (agent != null && agent.isOnNavMesh)
                {
                    if (stunCoroutine != null) StopCoroutine(stunCoroutine);
                    stunCoroutine = StartCoroutine(BriefStun(hitStunDuration));
                }
            }
            else
            {
                Debug.LogWarning($"[ZombieHealth] {gameObject.name} was hit but has NO Animator assigned!");
            }
        }
    }

    private IEnumerator BriefStun(float duration)
    {
        if (agent == null || !agent.isOnNavMesh) yield break;
        
        // Enter TakingDamage state to prevent AI updates from overriding the stun
        if (controller != null)
            controller.currentState = ZombieController.ZombieState.TakingDamage;
        
        agent.isStopped = true;
        agent.velocity = Vector3.zero;
        
        yield return new WaitForSeconds(duration);
        
        // Only recover if we haven't died in the meantime
        if (controller != null && controller.currentState == ZombieController.ZombieState.TakingDamage)
        {
            if (agent != null && agent.enabled && agent.isOnNavMesh) 
            {
                agent.isStopped = false;
            }
            controller.ForceAggro(); // Return to aggro/chasing
        }
    }
    private IEnumerator Die()
    {
        // Safety: Prevent crash if controller is still missing
        if (controller != null)
        {
            controller.currentState = ZombieController.ZombieState.Dead;
            controller.StopAudio(); // Stop looping growl
        }
        
        // Play death sound
        if (audioSource != null && deathSound != null)
        {
            audioSource.PlayOneShot(deathSound);
        }

        GameEvents.RaiseZombieDied(gameObject);

        TryDropLoot();

        if (anim != null) 
        {
            anim.SetTrigger("Die");
            anim.applyRootMotion = true; // Enable root motion so the death animation lowers the zombie to the floor
        }
        
        // Disable NavMeshAgent completely so it doesn't hold the zombie up
        if (agent != null) 
        {
            agent.isStopped = true;
            agent.enabled = false;
        }

        // Disable colliders so the capsule doesn't keep the model floating or block the player
        Collider[] colliders = GetComponents<Collider>();
        foreach(Collider col in colliders)
        {
            col.enabled = false;
        }

        // Disable rigidbody physics if present
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        if (controller != null && controller.zombieCombat != null) 
            controller.DisableHitbox();

        // Use the ZombieDissolve component to run the 1→0 death transition
        // across all materials. Falls back to a plain wait if not present.
        ZombieDissolve dissolve = GetComponent<ZombieDissolve>();
        if (dissolve != null)
        {
            yield return dissolve.DissolveDeath();
        }
        else
        {
            yield return new WaitForSeconds(2f);
        }

        Destroy(gameObject);
    }

    private void TryDropLoot()
    {
        if (Random.value > dropChance) 
        {
            Debug.Log($"[Loot] {gameObject.name} did not drop anything (rolled above {dropChance * 100}% chance).");
            return;
        }

        // Use Dynamic Loot logic to see what player needs most
        GameObject prefabToSpawn = GetMostNeededLoot();
        
        if (prefabToSpawn != null)
        {
            Debug.Log($"[Loot] {gameObject.name} spawning MOST NEEDED loot: {prefabToSpawn.name}");
            // Spawn slightly above the ground
            Vector3 spawnPos = transform.position + Vector3.up * 0.2f;
            Instantiate(prefabToSpawn, spawnPos, Quaternion.identity);
        }
        else
        {
            Debug.LogWarning($"[Loot] {gameObject.name} TRIED TO DROP LOOT, BUT PREFABS ARE NOT ASSIGNED IN THE INSPECTOR!");
        }
    }

    private GameObject GetMostNeededLoot()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return healthPickupPrefab;

        // 1. Calculate Health %
        PlayerHealth pHealth = player.GetComponent<PlayerHealth>();
        float healthPercent = (pHealth != null) ? (pHealth.GetCurrentHealth() / pHealth.GetMaxHealth()) : 1f;

        // 2. Calculate Ammo %
        PlayerAmmo pAmmo = player.GetComponent<PlayerAmmo>();
        float pistolPercent = 1f;
        float riflePercent = 1f;

        if (pAmmo != null)
        {
            pistolPercent = (float)pAmmo.GetAmmo(AmmoType.Pistol) / pAmmo.GetMaxAmmo(AmmoType.Pistol);
            riflePercent = (float)pAmmo.GetAmmo(AmmoType.Rifle) / pAmmo.GetMaxAmmo(AmmoType.Rifle);
        }

        // 3. Prioritize Health if it's dangerously low (below 40%)
        if (healthPercent < 0.4f) return healthPickupPrefab;

        // 4. Otherwise, pick the one that is at the lowest percentage
        if (healthPercent <= pistolPercent && healthPercent <= riflePercent)
            return healthPickupPrefab;
        
        if (pistolPercent <= riflePercent)
            return pistolAmmoPrefab;
        
        return rifleAmmoPrefab;
    }
}
