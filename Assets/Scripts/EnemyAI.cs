using UnityEngine;
using UnityEngine.AI;

public class EnemyAI : MonoBehaviour
{
    [Header("References")] 
    [SerializeField] private GameObject target;
    [SerializeField] private PatrolPath patrolPath;

    [Header("Enemy's State")] 
    [SerializeField] private EnemyState enemyState;

    [Header("Raycast Settings")] 
    [SerializeField] private Transform raycastPivot;
    
    [Header("Values")]
    [SerializeField] private float hostileDetectionRange = 15f;
    [SerializeField] private float suspicionDetectionRange = 25f;
    [SerializeField] private float waypointWaitTime = 3f;
    [SerializeField] private float suspicionTime = 3f;
    [SerializeField] private float normalSpeed = 3.5f;
    [SerializeField] private float chasingSpeed = 5f;
    [SerializeField] private float turnSpeed = 5f;

    private enum EnemyState
    {
        Friendly,
        Suspicion,
        Hostile
    }

    private float timeSinceLastSawPlayer = Mathf.Infinity;
    private float timeSinceArrivedAtWaypoint = Mathf.Infinity;
    private int currentWaypointIndex;

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
        PatrolBehaviour();
        if (IsTargetAccessibleCheck(hostileDetectionRange) && IsTargetVisibleCheck())
        {
            StartChasing();
        }
    }

    private void SuspicionState()
    {
        if (IsTargetAccessibleCheck(suspicionDetectionRange) && IsTargetVisibleCheck())
        {
            StartChasing();
        }
        
        if (timeSinceLastSawPlayer >= suspicionTime)
        {
            enemyState = EnemyState.Friendly;
        }
    }

    private void HostileState()
    {
        navMeshAgent.speed = chasingSpeed;

        FaceTarget();
        
        if (IsTargetVisibleCheck())
        {
            StartChasing();
        }
        else
        {
            enemyState = EnemyState.Suspicion;
        }
    }

    private void FaceTarget()
    {
        var direction = (target.transform.position - raycastPivot.position).normalized;
        var lookRotation = Quaternion.LookRotation(new Vector3(direction.x, 0f, direction.z));
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, turnSpeed * Time.deltaTime);
    }

    public void StartChasing()
    {
        enemyState = EnemyState.Hostile;
        timeSinceLastSawPlayer = 0f;
        navMeshAgent.SetDestination(target.transform.position);
    }
    
    private void PatrolBehaviour()
    {
        var distanceToWaypoint = Vector3.Distance(transform.position, GetCurrentWaypoint());
        
        if (distanceToWaypoint <= navMeshAgent.stoppingDistance)
        {
            timeSinceArrivedAtWaypoint = 0f;
            currentWaypointIndex = patrolPath.GetNextIndex(currentWaypointIndex);
        }
        
        enemyNextPosition = GetCurrentWaypoint();
        
        if (timeSinceArrivedAtWaypoint >= waypointWaitTime)
        {
            navMeshAgent.SetDestination(enemyNextPosition);
        }
    }

    private Vector3 GetCurrentWaypoint()
    {
        return patrolPath.GetCurrentWaypoint(currentWaypointIndex);
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
            if (Physics.Raycast(transform.position, direction, out var hit) && hit.collider.CompareTag("Player"))
            {
                return true;
            }
            return false;
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
        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(enemyPivot, direction);
    }
}
