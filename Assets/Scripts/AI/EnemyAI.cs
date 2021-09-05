using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

namespace Mouth.AI
{
    [RequireComponent(typeof(NavMeshAgent), typeof(Animator), typeof(Rigidbody))]
    public class EnemyAI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private CharacterController target;
        [SerializeField] private PatrolPath patrolPath;
    
        [Header("Raycast Settings")] 
        [SerializeField] private Transform playerRaycastPivot;
        [SerializeField] private Transform enemyRaycastPivot;

        [Header("Player Mask")] 
        [SerializeField] private LayerMask playerMask;
    
        [Header("Values"), Min(0)] 
        [SerializeField] private float hostileDetectionRange = 15f;
        [SerializeField] private float suspicionDetectionRange = 25f;
        [SerializeField] private float wanderRange = 7.5f;
        [SerializeField] private float waypointWaitTime = 3f;
        [SerializeField] private float normalSpeed = 3.5f;
        [SerializeField] private float chasingSpeed = 5f;
        [SerializeField] private float turnSpeed = 5f;
        [SerializeField, Range(1, 5)] private int wanderCheckpointAmount = 1;
        [SerializeField, Range(1, 2)] private float wanderCheckpointStep = 1f;

        private static readonly int Speed = Animator.StringToHash("Speed");
        private bool isInHostileRange;
        private bool isInSuspicionRange;
        private bool isVisible;
        private bool wasEnemyHit;

        private NavMeshAgent navMeshAgent;
        private Animator animator;
        private Coroutine activeCoroutine;
        private EnemyState oldState;
        private EnemyState newState
        {
            get => oldState;
            set
            {
                OnStateChange?.Invoke(oldState, value);
                oldState = value;
            }
        }

        private delegate void StateChangeEvent(EnemyState previousState,EnemyState nextState);
        private event StateChangeEvent OnStateChange;

        private void Awake()
        {
            animator = GetComponent<Animator>();
            navMeshAgent = GetComponent<NavMeshAgent>();

            oldState = EnemyState.Default;
        }

        private void OnEnable()
        {
            OnStateChange += HandleStateChange;
        }

        private void OnDisable()
        {
            OnStateChange -= HandleStateChange;
        }

        private void Start()
        {
            newState = EnemyState.Patrol;

            wanderRange = Mathf.Max(wanderRange, navMeshAgent.stoppingDistance * wanderCheckpointStep);
        }

        private void Update()
        {
            UpdateMovementAnimations();
        }

        private void FixedUpdate()
        {
            isInHostileRange = IsTargetAccessibleCheck(hostileDetectionRange);
            isInSuspicionRange = IsTargetAccessibleCheck(suspicionDetectionRange);
            isVisible = IsTargetVisibleCheck();

            switch (newState)
            {
                case EnemyState.Patrol:
                {
                    if (isInHostileRange && isVisible)
                    {
                        newState = EnemyState.Hostility;
                    }

                    break;
                }
                case EnemyState.Suspicion:
                {
                    if (isInSuspicionRange && isVisible)
                    {
                        newState = EnemyState.Hostility;
                    }

                    break;
                }
            }
        }

        private void HandleStateChange(EnemyState previousState, EnemyState nextState)
        {
            if (previousState == nextState) return;
        
            if (activeCoroutine != null)
            {
                StopCoroutine(activeCoroutine);
            }

            switch (nextState)
            {
                case EnemyState.Patrol:
                    activeCoroutine = StartCoroutine(PatrolBehaviour(nextState));
                    break;
                case EnemyState.Suspicion:
                    activeCoroutine = StartCoroutine(WanderBehaviour(nextState));
                    break;
                case EnemyState.Hostility:
                    activeCoroutine = StartCoroutine(HostileBehaviour(nextState));
                    break;
            }
        }

        private IEnumerator PatrolBehaviour(EnemyState state)
        {
            yield return new WaitUntil(() => newState == state);
        
            var patrolWaypointIndex = patrolPath.GetNextIndex(-1);
            var nextDestination = GetPatrolWaypoint(patrolWaypointIndex);
        
            navMeshAgent.speed = normalSpeed;
            navMeshAgent.SetDestination(nextDestination);

            while (enabled)
            {
                yield return new WaitWhile(() => navMeshAgent.pathPending);

                if (navMeshAgent.remainingDistance <= navMeshAgent.stoppingDistance)
                {
                    patrolWaypointIndex = patrolPath.GetNextIndex(patrolWaypointIndex);
                    nextDestination = GetPatrolWaypoint(patrolWaypointIndex);
                
                    yield return new WaitForSeconds(waypointWaitTime);
                
                    navMeshAgent.SetDestination(nextDestination);
                }

                yield return null;
            }
        }

        private IEnumerator WanderBehaviour(EnemyState state)
        {
            yield return new WaitUntil(() => newState == state);
        
            var wanderWaypointIndex = 1;
        
            navMeshAgent.speed = normalSpeed;
            navMeshAgent.SetDestination(RandomWanderPos());

            while(wanderWaypointIndex <= wanderCheckpointAmount)
            {
                yield return new WaitWhile(() => navMeshAgent.pathPending);
            
                if (navMeshAgent.remainingDistance <= navMeshAgent.stoppingDistance)
                {
                    wanderWaypointIndex++;
                
                    yield return new WaitForSeconds(waypointWaitTime);

                    navMeshAgent.SetDestination(RandomWanderPos());
                }

                yield return null;
            }
        
            newState = EnemyState.Patrol;
        }
    
        private IEnumerator HostileBehaviour(EnemyState state)
        {
            yield return new WaitUntil(() => newState == state);
        
            navMeshAgent.speed = chasingSpeed;
            navMeshAgent.SetDestination(target.transform.position);
        
            while (enabled)
            {
                yield return new WaitWhile(() => navMeshAgent.pathPending);

                if (wasEnemyHit)
                {
                    yield return FaceTargetWhenHit();
                    wasEnemyHit = false;
                }

                if (isVisible)
                {
                    navMeshAgent.SetDestination(target.transform.position);
                    FaceTarget();
                }
                else if (navMeshAgent.remainingDistance <= navMeshAgent.stoppingDistance)
                {
                    yield return CheckForPlayerWhenLost(waypointWaitTime);
                }
            
                yield return null;
            }
        }

        private IEnumerator CheckForPlayerWhenLost(float timer)
        {
            for (var i = 0f; i <= timer; i += Time.deltaTime)
            {
                if (isVisible)
                {
                    yield break;
                }

                yield return null;
            }
        
            newState = EnemyState.Suspicion;
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
            while (Vector3.Distance(transform.position, randomDirection) < navMeshAgent.stoppingDistance * wanderCheckpointStep);

            NavMesh.SamplePosition(randomDirection, out var navHit, wanderRange, NavMesh.AllAreas);

            return navHit.position;
        }
    
        private Vector3 GetPatrolWaypoint(int index)
        {
            return patrolPath.GetRandomWaypoint(index);
        }
    
        private bool IsTargetAccessibleCheck(float range)
        {
            var resultCollider = new Collider[1];
            var hitCollider = Physics.OverlapSphereNonAlloc(transform.position, range, resultCollider, playerMask);

            return hitCollider > 0;
        }

        private bool IsTargetVisibleCheck()
        {
            var forward = transform.TransformDirection(Vector3.forward);
            var direction = (playerRaycastPivot.position - enemyRaycastPivot.position).normalized;
        
            if (Vector3.Dot(forward, direction) >= 0f)
            {
                return Physics.Raycast(enemyRaycastPivot.position, direction, out var hit) && hit.collider.CompareTag("Player");
            }
        
            return false;
        }

        private IEnumerator FaceTargetWhenHit()
        {
            var direction = (playerRaycastPivot.position - enemyRaycastPivot.position).normalized;
            var lookRotation = Quaternion.LookRotation(new Vector3(direction.x, 0f, direction.z));

            while (Quaternion.Angle(transform.rotation, lookRotation) > 45f)
            {
                direction = (playerRaycastPivot.position - enemyRaycastPivot.position).normalized;
                lookRotation = Quaternion.LookRotation(new Vector3(direction.x, 0f, direction.z));
                transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, turnSpeed * Time.deltaTime);

                yield return null;
            }
        }

        private void FaceTarget()
        {
            var direction = (playerRaycastPivot.position - enemyRaycastPivot.position).normalized;
            var lookRotation = Quaternion.LookRotation(new Vector3(direction.x, 0f, direction.z));
        
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, turnSpeed * Time.deltaTime);
        }

        private void UpdateMovementAnimations()
        {
            var localVelocity = transform.InverseTransformDirection(navMeshAgent.velocity);
            var speed = localVelocity.z;
        
            animator.SetFloat(Speed, speed);
        }

        public void ChaseWhenHit()
        {
            wasEnemyHit = true;
            newState = EnemyState.Hostility;
        }

        public EnemyState GetEnemyState()
        {
            return newState;
        }
    
        private void OnDrawGizmosSelected()
        {
            var enemy = transform.position;
            var enemyPivot = enemyRaycastPivot.position;

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(enemy, hostileDetectionRange);
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(enemy, suspicionDetectionRange);
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(enemy, wanderRange);
            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(enemyPivot, playerRaycastPivot.position - enemyPivot);
        }
    }
}
