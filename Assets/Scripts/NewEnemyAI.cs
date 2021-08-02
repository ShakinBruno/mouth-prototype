using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class NewEnemyAI : MonoBehaviour
{
    [SerializeField] private Transform target;

    private NavMeshAgent navMeshAgent;
    private Animator animator;
    private static readonly int Speed = Animator.StringToHash("Speed");

    private Coroutine followCoroutine;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        navMeshAgent = GetComponent<NavMeshAgent>();
    }

    private void StartChasing()
    {
        if (followCoroutine == null)
        {
            followCoroutine = StartCoroutine(FollowTarget());
        }
        else
        {
            Debug.LogWarning("StartChasing was called on enemy that is already chasing. Possible bug.");
        }
    }

    private void Update()
    {
        UpdateMovementAnimations();   
    }
    
    private void UpdateMovementAnimations()
    {
        var localVelocity = transform.InverseTransformDirection(navMeshAgent.velocity);
        var speed = localVelocity.z;
        animator.SetFloat(Speed, speed);
    }

    private IEnumerator FollowTarget()
    {
        while (enabled)
        {
            navMeshAgent.SetDestination(target.position);
            yield return null;
        }
    }
}
