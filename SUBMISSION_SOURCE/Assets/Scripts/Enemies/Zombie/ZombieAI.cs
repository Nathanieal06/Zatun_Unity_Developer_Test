using UnityEngine;
using UnityEngine.AI;

public class ZombieAI : MonoBehaviour
{
    public static bool IsCutsceneActive { get; set; } = false;

    private ZombieController controller;
    private Animator anim;
    private NavMeshAgent agent;
    private Transform player;
    private PlayerHealth playerHealth;

    private float aggroRange;
    private float deAggroRange;
    private float roamRadius;
    private float fieldOfView;
    private float walkSpeed;
    private float runSpeed;
    private float minIdleTime;
    private float maxIdleTime;
    private float loseInterestTime;

    private float idleTimer;
    private float timeSinceLastSeen;
    private float damageAggroTimer;

    public void Initialize(ZombieController controller, Animator anim, NavMeshAgent agent, Transform player, PlayerHealth playerHealth)
    {
        this.controller = controller;
        this.anim = anim;
        this.agent = agent;
        this.player = player;
        this.playerHealth = playerHealth;

        this.aggroRange = controller.aggroRange;
        this.deAggroRange = controller.deAggroRange;
        this.roamRadius = controller.roamRadius;
        this.fieldOfView = controller.fieldOfView;
        this.walkSpeed = controller.walkSpeed;
        this.runSpeed = controller.runSpeed;
        this.minIdleTime = controller.minIdleTime;
        this.maxIdleTime = controller.maxIdleTime;
        this.loseInterestTime = controller.loseInterestTime;

        controller.currentState = ZombieController.ZombieState.Idle;
        idleTimer = Random.Range(minIdleTime, maxIdleTime);
    }

    public float Get2DDistanceToPlayer()
    {
        if (player == null) return float.MaxValue;
        Vector3 p1 = transform.position;
        Vector3 p2 = player.position;
        p1.y = 0; p2.y = 0;
        return Vector3.Distance(p1, p2);
    }

    public bool CheckLineOfSight()
    {
        if (IsCutsceneActive) return false;
        if (player == null) return false;
        float distanceToPlayer = Get2DDistanceToPlayer();
        if (distanceToPlayer < 2.0f) return true;

        if (controller.currentState != ZombieController.ZombieState.Aggro && distanceToPlayer > aggroRange) return false;
        if (controller.currentState == ZombieController.ZombieState.Aggro && distanceToPlayer > deAggroRange) return false;

        Vector3 dirToPlayer = (player.position - transform.position).normalized;
        if (controller.currentState != ZombieController.ZombieState.Aggro)
        {
            float angle = Vector3.Angle(transform.forward, dirToPlayer);
            if (angle > fieldOfView / 2f) return false;
        }

        Vector3 rayOrigin = transform.position + Vector3.up * 1.5f;
        Vector3 rayTarget = player.position + Vector3.up * 1.5f;
        Vector3 rayDir = (rayTarget - rayOrigin).normalized;
        float dist = Vector3.Distance(rayOrigin, rayTarget);

        // Visual debug for Line of Sight
        Debug.DrawRay(rayOrigin, rayDir * dist, Color.red);

        RaycastHit[] hits = Physics.RaycastAll(rayOrigin, rayDir, dist);
        float closestDist = float.MaxValue;
        bool hitPlayer = false;

        foreach (RaycastHit hit in hits)
        {
            bool isPlayer = (hit.collider.CompareTag("Player") || hit.collider.GetComponentInParent<PlayerHealth>() != null);
            if ((hit.collider.isTrigger && !isPlayer) || hit.transform.root == transform.root) continue;
            if (hit.distance < closestDist)
            {
                closestDist = hit.distance;
                // Check for tag OR specific component to be extra safe
                hitPlayer = (hit.collider.CompareTag("Player") || hit.collider.GetComponentInParent<PlayerHealth>() != null);
            }
        }
        return hitPlayer;
    }

    public void HandleIdle()
    {
        if (agent.isActiveAndEnabled && agent.isOnNavMesh && !agent.isStopped) agent.isStopped = true;

        if (CheckLineOfSight())
        {
            if (agent.isActiveAndEnabled && agent.isOnNavMesh) agent.isStopped = false;
            controller.currentState = ZombieController.ZombieState.Aggro;
            timeSinceLastSeen = 0f;
            return;
        }

        idleTimer -= Time.deltaTime;
        if (idleTimer <= 0f)
        {
            controller.currentState = ZombieController.ZombieState.Roaming;
            if (agent.isActiveAndEnabled && agent.isOnNavMesh)
            {
                agent.isStopped = false;
                agent.SetDestination(GetRoamPoint(roamRadius));
            }
        }
    }

    public void HandleRoam()
    {
        if (agent.isActiveAndEnabled && agent.isOnNavMesh) agent.speed = walkSpeed;

        if (CheckLineOfSight())
        {
            controller.currentState = ZombieController.ZombieState.Aggro;
            timeSinceLastSeen = 0f;
            return;
        }

        if (agent.isActiveAndEnabled && agent.isOnNavMesh && !agent.pathPending && (agent.remainingDistance < 0.5f || !agent.hasPath))
        {
            controller.currentState = ZombieController.ZombieState.Idle;
            idleTimer = Random.Range(minIdleTime, maxIdleTime);
        }
    }

    public void HandleAggro()
    {
        if (agent.isActiveAndEnabled && agent.isOnNavMesh) agent.speed = runSpeed;
        float distanceToPlayer = Get2DDistanceToPlayer();

        if (damageAggroTimer > 0f)
        {
            damageAggroTimer -= Time.deltaTime;
        }
        else if (distanceToPlayer > deAggroRange)
        {
            controller.currentState = ZombieController.ZombieState.Idle;
            idleTimer = Random.Range(minIdleTime, maxIdleTime);
            if (agent.isActiveAndEnabled && agent.isOnNavMesh)
            {
                agent.isStopped = true;
                agent.velocity = Vector3.zero;
            }
            if (anim != null) anim.applyRootMotion = false;
            return;
        }

        bool inRangeForAnyAttack = false;
        if (controller.attacks != null)
        {
            foreach (var attack in controller.attacks)
            {
                if (distanceToPlayer <= attack.range)
                {
                    inRangeForAnyAttack = true;
                    break;
                }
            }
        }

        if (inRangeForAnyAttack && controller.zombieCombat.attackTimer <= 0f && CheckLineOfSight())
        {
            if (anim != null) anim.applyRootMotion = true;
            if (agent.isActiveAndEnabled && agent.isOnNavMesh)
            {
                agent.isStopped = true;
                agent.velocity = Vector3.zero;
            }
            controller.currentState = ZombieController.ZombieState.Attacking;
            controller.zombieCombat.PerformRandomAttack();
            return;
        }

        if (CheckLineOfSight())
        {
            timeSinceLastSeen = 0f;
            if (agent.isActiveAndEnabled && agent.isOnNavMesh)
            {
                if (agent.isStopped) agent.isStopped = false;
                agent.SetDestination(player.position);
            }
        }
        else
        {
            timeSinceLastSeen += Time.deltaTime;
            if (timeSinceLastSeen > loseInterestTime)
            {
                controller.currentState = ZombieController.ZombieState.Idle;
                idleTimer = Random.Range(minIdleTime, maxIdleTime);
                if (agent.isActiveAndEnabled && agent.isOnNavMesh)
                {
                    agent.isStopped = true;
                    agent.velocity = Vector3.zero;
                }
                if (anim != null) anim.applyRootMotion = false;
                return;
            }
        }
    }

    public Vector3 GetRoamPoint(float range)
    {
        Vector3 randomDir = Random.insideUnitSphere * range;
        randomDir += transform.position;
        NavMeshHit hit;
        NavMesh.SamplePosition(randomDir, out hit, range, NavMesh.AllAreas);
        return hit.position;
    }

    public void OnDamaged()
    {
        damageAggroTimer = 5f;
        timeSinceLastSeen = 0f;
    }
}
