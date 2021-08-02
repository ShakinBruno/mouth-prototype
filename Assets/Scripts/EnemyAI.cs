using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

public class EnemyAI : MonoBehaviour
{
    [Header("References")] 
    [SerializeField] private GameObject target;
    [SerializeField] private PatrolPath patrolPath;

    [Header("Enemy's State")] 
    public EnemyState enemyState;

    [Header("Raycast Settings")] 
    [SerializeField] private Transform raycastPivot;
    
    [Header("Values"), Min(0)]
    [SerializeField] private float hostileDetectionRange = 15f;
    [SerializeField] private float suspicionDetectionRange = 25f;
    [SerializeField] private float wanderRange = 7.5f;
    [SerializeField] private float waypointWaitTime = 3f;
    [SerializeField] private float suspicionTime = 3f;
    [SerializeField] private float normalSpeed = 3.5f;
    [SerializeField] private float chasingSpeed = 5f;
    [SerializeField] private float turnSpeed = 5f;
    [SerializeField, Range(1, 5)] private int wanderCheckpointsAmount = 1;
    [SerializeField, Range(1, 2)] private float wanderCheckpointsStep = 1f;

    private float timeSinceLastSawPlayer = Mathf.Infinity;
    private float timeSinceArrivedAtWaypoint = Mathf.Infinity;
    private int currentWaypointIndex;
    private int wanderWaypointIndex = 1;
    private bool hasCoroutineFinished = true;

    private NavMeshAgent navMeshAgent;
    private Vector3 enemyNextPosition;

    private void Awake()
    {
        navMeshAgent = GetComponent<NavMeshAgent>();
    }

    private void Start()
    {
        enemyState = EnemyState.Friendly;
        enemyNextPosition = transform.position;
        currentWaypointIndex = Random.Range(0, patrolPath.waypoints.Count);
        navMeshAgent.speed = normalSpeed;

        if (wanderRange < navMeshAgent.stoppingDistance * 2f)
        {
            wanderRange = navMeshAgent.stoppingDistance * 2f;
        }
    }

    private void Update()
    {
        EnemyStateBehaviour();

        UpdateTimers();
    }

    private void UpdateTimers()
    {
        timeSinceLastSawPlayer += Time.deltaTime;
        timeSinceArrivedAtWaypoint += Time.deltaTime;
    }

    private void EnemyStateBehaviour()
    {
        if (enemyState == EnemyState.Friendly)
        {
            FriendlyState();
        }
        else if (enemyState == EnemyState.Suspicion)
        {
            SuspicionState();
        }
        else if (enemyState == EnemyState.Hostile)
        {
            HostileState();
        }
    }

    private void FriendlyState()
    {
        navMeshAgent.speed = normalSpeed;
        
        ResetWanderStats();
        PatrolBehaviour();
        
        if (IsTargetAccessibleCheck(hostileDetectionRange) && IsTargetVisibleCheck())
        {
            enemyState = EnemyState.Hostile;
        }
    }

    private void SuspicionState()
    {
        if (IsTargetAccessibleCheck(suspicionDetectionRange) && IsTargetVisibleCheck())
        {
            enemyState = EnemyState.Hostile;
        }

        if (timeSinceLastSawPlayer >= suspicionTime && hasCoroutineFinished)
        {
            StartCoroutine(WanderBehaviour());
        }
    }

    private void HostileState()
    {
        navMeshAgent.speed = chasingSpeed;
        timeSinceLastSawPlayer = 0f;
        
        ResetWanderStats();
        FaceTarget();
        
        navMeshAgent.SetDestination(target.transform.position);
        
        if (IsTargetVisibleCheck())
        {
            enemyState = EnemyState.Hostile;
        }
        else
        {
            enemyState = EnemyState.Suspicion;
        }
    }

    private void ResetWanderStats()
    {
        StopAllCoroutines();
        hasCoroutineFinished = true;
        wanderWaypointIndex = 1;
    }

    private void FaceTarget()
    {
        var direction = (target.transform.position - raycastPivot.position).normalized;
        var lookRotation = Quaternion.LookRotation(new Vector3(direction.x, 0f, direction.z));
        
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, turnSpeed * Time.deltaTime);
    }

    private void PatrolBehaviour()
    {
        var distanceToWaypoint = Vector3.Distance(transform.position, GetCurrentWaypoint(currentWaypointIndex));
        
        if (distanceToWaypoint <= navMeshAgent.stoppingDistance)
        {
            timeSinceArrivedAtWaypoint = 0f;
            currentWaypointIndex = patrolPath.GetNextIndex(currentWaypointIndex);
        }
        
        enemyNextPosition = GetCurrentWaypoint(currentWaypointIndex);
        
        if (timeSinceArrivedAtWaypoint >= waypointWaitTime)
        {
            navMeshAgent.SetDestination(enemyNextPosition);
        }
    }

    private IEnumerator WanderBehaviour()
    {
        hasCoroutineFinished = false;
        
        navMeshAgent.SetDestination(RandomWanderPos());

        while(true)
        {
            if (navMeshAgent.remainingDistance <= navMeshAgent.stoppingDistance)
            {
                wanderWaypointIndex++;
                
                yield return new WaitForSeconds(waypointWaitTime);

                if (wanderWaypointIndex > wanderCheckpointsAmount)
                {
                    enemyState = EnemyState.Friendly;
                    break;
                }
                
                navMeshAgent.SetDestination(RandomWanderPos());
            }
            else
            {
                yield return null;
            }
        }
    }
    
    private Vector3 RandomWanderPos()
    {
        Vector3 randomDirection;
        do
        {
            randomDirection = Random.insideUnitSphere * wanderRange;
            randomDirection.y = Mathf.Clamp01(randomDirection.y);
            randomDirection += transform.position;
        } 
        while (Vector3.Distance(transform.position, randomDirection) < navMeshAgent.stoppingDistance * wanderCheckpointsStep);

        NavMesh.SamplePosition(randomDirection, out var navHit, wanderRange, NavMesh.AllAreas);

        return navHit.position;
    }

    private Vector3 GetCurrentWaypoint(int index)
    {
        return patrolPath.GetCurrentWaypoint(index);
    }

    private bool IsTargetAccessibleCheck(float range)
    {
        var hitColliders = Physics.OverlapSphere(transform.position, range);
        foreach (var hitCollider in hitColliders)
        {
            if (hitCollider.CompareTag("Player"))
            {
                return true;
            }
        }
        return false;
    }

    private bool IsTargetVisibleCheck()
    {
        var forward = transform.TransformDirection(Vector3.forward);
        var direction = (target.transform.position - raycastPivot.position).normalized;
        if (Vector3.Dot(forward, direction) >= 0f)
        {
            return Physics.Raycast(transform.position, direction, out var hit) && hit.collider.CompareTag("Player");
        }
        return false;
    }

    private void OnDrawGizmosSelected()
    {
        var enemy = transform.position;
        var enemyPivot = raycastPivot.position;
        var direction = target.transform.position - enemy;
        
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(enemy, hostileDetectionRange);
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(enemy, suspicionDetectionRange);
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(enemy, wanderRange);
        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(enemyPivot, direction);
    }
}
