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
        [SerializeField] private PatrolPath patrolPath;
    
        [Header("Raycast References")]
        [SerializeField] private Transform raycastOrigin;
        [SerializeField] private Transform[] raycastTargets;

        [Header("Player Mask")] 
        [SerializeField] private LayerMask playerMask;
    
        [Header("Values"), Min(0)] 
        [SerializeField] private float hostileDetectionRange = 15f;
        [SerializeField] private float suspicionDetectionRange = 25f;
        [SerializeField] private float wanderRange = 7.5f;
        [SerializeField] private float angleOfDetection = 70f;
        [SerializeField] private float raycastUpdateRate = 0.2f;
        [SerializeField] private float waypointWaitTime = 3f;
        [SerializeField] private float normalSpeed = 3.5f;
        [SerializeField] private float chasingSpeed = 5f;
        [SerializeField] private float turnSpeed = 5f;
        [SerializeField] private float raycastGizmoRadius = 0.05f;
        [SerializeField, Range(1, 5)] private int wanderCheckpointAmount = 1;
        [SerializeField, Range(1, 2)] private float wanderCheckpointStep = 1f;

        private static readonly int Speed = Animator.StringToHash("Speed");
        private int currentRaycastIndex;
        private bool wasEnemyHit;

        private CharacterController target;
        private NavMeshAgent navMeshAgent;
        private Animator animator;
        private Coroutine activeCoroutine;
        private Vector3 currentRaycastPos;
        private Vector3 nextRaycastPos;
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
            target = GameObject.FindWithTag("Player").GetComponent<CharacterController>();
            animator = GetComponent<Animator>();
            navMeshAgent = GetComponent<NavMeshAgent>();

            oldState = EnemyState.Default;
            wanderRange = Mathf.Max(wanderRange, navMeshAgent.stoppingDistance * wanderCheckpointStep);
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
            currentRaycastPos = raycastTargets[currentRaycastIndex].position;
            nextRaycastPos = raycastTargets[GetNextRaycastIndex(currentRaycastIndex)].position;
        }

        private void Update()
        {
            UpdateMovementAnimations();
        }

        private void FixedUpdate()
        {
            switch (newState)
            {
                case EnemyState.Patrol:
                {
                    CheckForVisibility(hostileDetectionRange);
                    break;
                }
                case EnemyState.Suspicion:
                {
                    CheckForVisibility(suspicionDetectionRange);
                    break;
                }
            }
        }

        private void CheckForVisibility(float range)
        {
            if (IsTargetInRange(range) && IsTargetInFrontOf() && IsTargetVisibleCheck())
            {
                newState = EnemyState.Hostility;
            }
        }
        
        private bool IsTargetInRange(float range)
        {
            var resultCollider = new Collider[1];
            var hitCollider = Physics.OverlapSphereNonAlloc(transform.position, range, resultCollider, playerMask);

            return hitCollider > 0;
        }

        private bool IsTargetInFrontOf()
        {
            var forward = transform.TransformDirection(Vector3.forward).normalized;
            var direction = (target.transform.position - transform.position).normalized;

            return Vector3.Angle(forward, direction) <= angleOfDetection;
        }

        private bool IsTargetVisibleCheck()
        {
            UpdateRaycastPosition();

            var originPos = raycastOrigin.position;
            var direction = (currentRaycastPos - originPos).normalized;
            
            return Physics.Raycast(originPos, direction, out var hit) && hit.collider.CompareTag("Player");
        }

        private void UpdateRaycastPosition()
        {
            if (currentRaycastPos == nextRaycastPos)
            {
                currentRaycastIndex = GetNextRaycastIndex(currentRaycastIndex);
                nextRaycastPos = raycastTargets[GetNextRaycastIndex(currentRaycastIndex)].position;
            }
            else
            {
                currentRaycastPos = Vector3.MoveTowards(currentRaycastPos, nextRaycastPos, raycastUpdateRate);
            }
        }

        private void HandleStateChange(EnemyState previousState, EnemyState nextState)
        {
            if (previousState == nextState) return;
        
            if (activeCoroutine != null)
            {
                StopCoroutine(activeCoroutine);
            }

            if (nextState == EnemyState.Patrol)
            {
                activeCoroutine = StartCoroutine(PatrolBehaviour(nextState));
            }
            else if (nextState == EnemyState.Suspicion)
            {
                activeCoroutine = StartCoroutine(WanderBehaviour(nextState));
            }
            else if (nextState == EnemyState.Hostility)
            {
                activeCoroutine = StartCoroutine(HostileBehaviour(nextState));
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

                if (IsTargetVisibleCheck())
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
                if (IsTargetVisibleCheck())
                {
                    yield break;
                }

                yield return null;
            }
        
            newState = EnemyState.Suspicion;
        }

        private Vector3 RandomWanderPos()
        {
            NavMeshHit navHit;
            
            do
            {
                var randomDirection = Random.insideUnitSphere * wanderRange;
                
                randomDirection.y = Mathf.Clamp01(randomDirection.y);
                randomDirection += transform.position;
                
                NavMesh.SamplePosition(randomDirection, out navHit, wanderRange, NavMesh.AllAreas);
            } 
            while (Vector3.Distance(transform.position, navHit.position) <= navMeshAgent.stoppingDistance * wanderCheckpointStep);

            return navHit.position;
        }

        private int GetNextRaycastIndex(int index)
        {
            return (index + 1) % raycastTargets.Length;
        }
    
        private Vector3 GetPatrolWaypoint(int index)
        {
            return patrolPath.GetRandomWaypoint(index);
        }

        private IEnumerator FaceTargetWhenHit()
        {
            var direction = (target.transform.position - transform.position).normalized;
            var lookRotation = Quaternion.LookRotation(new Vector3(direction.x, 0f, direction.z));

            while (Quaternion.Angle(transform.rotation, lookRotation) > 45f)
            {
                direction = (target.transform.position - transform.position).normalized;
                lookRotation = Quaternion.LookRotation(new Vector3(direction.x, 0f, direction.z));
                transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, turnSpeed * Time.deltaTime);

                yield return null;
            }
        }

        private void FaceTarget()
        {
            var direction = (target.transform.position - transform.position).normalized;
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

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(enemy, hostileDetectionRange);
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(enemy, suspicionDetectionRange);
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(enemy, wanderRange);

            for (var i = 0; i < raycastTargets.Length; i++)
            {
                var j = GetNextRaycastIndex(i);
                
                Gizmos.color = Color.yellow;
                Gizmos.DrawSphere(raycastTargets[i].position, raycastGizmoRadius);
                Gizmos.color = Color.black;
                Gizmos.DrawLine(raycastTargets[i].position, raycastTargets[j].position);
            }
        }
    }
}
