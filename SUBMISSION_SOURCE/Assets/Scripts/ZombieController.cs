using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(ZombieHealth))]
[RequireComponent(typeof(ZombieCombat))]
[RequireComponent(typeof(ZombieAI))]
public class ZombieController : MonoBehaviour
{
    public enum ZombieState { Spawning, Idle, Roaming, Aggro, Attacking, TakingDamage, Dead }
    public ZombieState currentState;

    [Header("AI Settings")]
    public float aggroRange = 10f;
    public float deAggroRange = 18f;
    public float roamRadius = 15f;
    public float maxHealth = 100f;
    
    [Header("Vision & Movement")]
    public float fieldOfView = 120f;
    public float walkSpeed = 1.5f;
    public float runSpeed = 4f;
    public float minIdleTime = 2f;
    public float maxIdleTime = 5f;
    public float loseInterestTime = 4f;

    [Header("Combat Settings")]
    public AttackData[] attacks;
    public Collider attackHitbox;
    public bool useAutoHitbox = true;

    public Transform acidSpawnPoint;
    public GameObject acidProjectilePrefab;
    public float projectileForce = 15f;

    [Header("Audio")]
    public AudioClip growlSound;
    private AudioSource audioSource;

    [Header("Visual Fixes")]
    public float groundOffset = 0f;

    [HideInInspector] public NavMeshAgent agent;
    private Animator anim;
    private Transform player;
    private PlayerHealth playerHealth;

    [HideInInspector] public ZombieHealth zombieHealth;
    [HideInInspector] public ZombieCombat zombieCombat;
    [HideInInspector] public ZombieAI zombieAI;

    void Awake()
    {
        zombieHealth = GetComponent<ZombieHealth>();
        zombieCombat = GetComponent<ZombieCombat>();
        zombieAI = GetComponent<ZombieAI>();
        
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponentInChildren<Animator>();

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 1f; // 3D sound
            audioSource.loop = true;
            audioSource.playOnAwake = false;
            audioSource.minDistance = 2f;
            audioSource.maxDistance = 20f;
        }

        if (growlSound != null)
        {
            audioSource.clip = growlSound;
            audioSource.Play();
        }
        
        if (anim == null)
        {
            Debug.LogError($"[ZombieController] No Animator found on {gameObject.name} or its children! Hit/Death animations will not play.");
        }
    }

    public void StopAudio()
    {
        if (audioSource != null) audioSource.Stop();
    }

    void OnEnable() => GameEvents.OnPlayerDied += HandlePlayerDied;
    void OnDisable() => GameEvents.OnPlayerDied -= HandlePlayerDied;

    void HandlePlayerDied()
    {
        currentState = ZombieState.Roaming;
        if (anim != null) anim.applyRootMotion = false;
        if (agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh)
        {
            agent.isStopped = false;
            agent.SetDestination(zombieAI.GetRoamPoint(roamRadius));
        }
    }

    void Start()
    {
        if (attacks == null || attacks.Length == 0)
        {
            attacks = new AttackData[] {
                new AttackData { animTrigger = "Attack1", damage = 10f, range = 1.5f, isRanged = false, cooldown = 2f, hitboxDelay = 0.4f, hitboxDuration = 0.3f }
            };
        }
        
        foreach (var attack in attacks)
        {
            Debug.Log($"[ZombieController] {gameObject.name} registered attack: {attack.animTrigger} (Dmg: {attack.damage}, Range: {attack.range})");
        }

        if (anim != null) anim.applyRootMotion = false;

        if (agent != null)
        {
            agent.baseOffset = groundOffset;
            float maxAttackRange = 0f;
            foreach (var attack in attacks) if (attack.range > maxAttackRange) maxAttackRange = attack.range;
            agent.stoppingDistance = Mathf.Min(0.5f, maxAttackRange * 0.5f);
        }

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
            playerHealth = playerObj.GetComponentInChildren<PlayerHealth>();
            Debug.Log($"[ZombieController] {gameObject.name} found Player: {playerObj.name} with tag {playerObj.tag}");
        }
        else
        {
            Debug.LogError($"[ZombieController] {gameObject.name} could NOT find any GameObject with tag 'Player'!");
        }

        if (attackHitbox == null)
        {
            Transform hbTrans = transform.Find("ZombieAttackHitBox");
            if (hbTrans != null) attackHitbox = hbTrans.GetComponent<Collider>();
            if (attackHitbox == null)
            {
                foreach (Collider c in GetComponentsInChildren<Collider>())
                {
                    if (c.gameObject.name.Contains("HitBox")) { attackHitbox = c; break; }
                }
            }
        }

        if (attackHitbox != null)
        {
            attackHitbox.enabled = false;
            attackHitbox.isTrigger = true;

            Rigidbody hbRb = attackHitbox.gameObject.GetComponent<Rigidbody>();
            if (hbRb == null) hbRb = attackHitbox.gameObject.AddComponent<Rigidbody>();
            hbRb.isKinematic = true; hbRb.useGravity = false;

            ZombieHitbox hbScript = attackHitbox.gameObject.GetComponent<ZombieHitbox>();
            if (hbScript == null) hbScript = attackHitbox.gameObject.AddComponent<ZombieHitbox>();
            hbScript.zombieController = this;
        }

        zombieHealth.Initialize(this, anim, agent, maxHealth);
        zombieCombat.Initialize(this, anim, player, playerHealth);
        zombieAI.Initialize(this, anim, agent, player, playerHealth);

        // Force snap to NavMesh floor on spawn
        if (agent != null && agent.isActiveAndEnabled)
        {
            NavMeshHit hit;
            if (NavMesh.SamplePosition(transform.position, out hit, 5.0f, NavMesh.AllAreas))
            {
                transform.position = hit.position;
            }
        }
    }

    void Update()
    {
        if (UIManager.IsPaused || ZombieAI.IsCutsceneActive)
        {
            if (agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh && !agent.isStopped)
            {
                agent.isStopped = true;
            }
            return;
        }

        if (currentState == ZombieState.Dead) return;
        if (player == null || (playerHealth != null && playerHealth.IsDead())) return;

        if (anim != null && agent != null)
        {
            // Don't override Blend while TakingDamage — let the flinch animation play freely.
            // ForceAggro() will directly push Blend=1 the moment the stun ends.
            if (currentState != ZombieState.TakingDamage)
            {
                float targetBlend = (currentState == ZombieState.Aggro || currentState == ZombieState.Attacking) ? 1.0f : 0.5f;
                float currentBlend = 0f;
                if (agent.isActiveAndEnabled && agent.isOnNavMesh)
                {
                    if (agent.speed > 0.01f)
                    {
                        currentBlend = (agent.desiredVelocity.magnitude / agent.speed) * targetBlend;
                    }
                    if (agent.isStopped || currentState == ZombieState.Attacking) currentBlend = 0f;
                }
                else if (currentState == ZombieState.Attacking)
                {
                    currentBlend = 0f;
                }

                anim.SetFloat("Blend", currentBlend);
                anim.SetBool("IsAggro", currentState == ZombieState.Aggro || currentState == ZombieState.Attacking);
            }
        }

        switch (currentState)
        {
            case ZombieState.Idle: zombieAI.HandleIdle(); break;
            case ZombieState.Roaming: zombieAI.HandleRoam(); break;
            case ZombieState.Aggro: zombieAI.HandleAggro(); break;
            case ZombieState.Attacking: zombieCombat.HandleAttack(); break;
        }
    }

    public void TakeDamage(float damage) => zombieHealth.TakeDamage(damage);
    public void EnableHitbox() => zombieCombat.EnableHitbox();
    public void DisableHitbox() => zombieCombat.DisableHitbox();
    public void SpawnAcidProjectile() => zombieCombat.SpawnAcidProjectile();

    public void ForceAggro()
    {
        if (currentState != ZombieState.Dead && player != null)
        {
            currentState = ZombieState.Aggro;
            if (agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh)
            {
                agent.isStopped = false;
                agent.speed = runSpeed;
                agent.SetDestination(player.position);
            }
            if (zombieAI != null)
            {
                zombieAI.OnDamaged();
            }
            // Immediately force the Animator into the running state.
            // desiredVelocity takes one frame to update after SetDestination,
            // so we push Blend=1 directly here to avoid a one-frame idle flash.
            if (anim != null)
            {
                anim.SetBool("IsAggro", true);
                anim.SetFloat("Blend", 1f);
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        PlayerHealth pHealth = other.GetComponentInParent<PlayerHealth>();
        if (other.CompareTag("Player") || pHealth != null)
        {
            if (currentState == ZombieState.Roaming || currentState == ZombieState.Idle) ForceAggro();
        }
    }
}
