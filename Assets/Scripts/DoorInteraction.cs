using System.Collections;
using UnityEngine;

public class DoorInteraction : MonoBehaviour
{
    [SerializeField] private Vector3 destination;
    [SerializeField] private float doorSpeed = 5f;
    
    [HideInInspector] public bool isOpened;

    private Vector3 origin;
    private Coroutine activeCoroutine;
    private Transform doorChild;

    public delegate void InteractionEvent(bool isDoorOpened);
    public InteractionEvent ONInteractionChange;

    private void Awake()
    {
        ONInteractionChange += HandleDoorStatusChange;
    }

    private void Start()
    {
        doorChild = transform.GetChild(0);
        origin = doorChild.position;
    }

    private void HandleDoorStatusChange(bool isDoorOpened)
    {
        if (activeCoroutine != null)
        {
            StopCoroutine(activeCoroutine);
        }
            
        if (!isDoorOpened)
        {
            activeCoroutine = StartCoroutine(StartDoorMoving(destination, true));
        }
        else
        {
            activeCoroutine = StartCoroutine(StartDoorMoving(origin, false));
        }
    }

    private IEnumerator StartDoorMoving(Vector3 finalPosition, bool openStatus)
    {
        while (doorChild.position != finalPosition) 
        {
            doorChild.position = Vector3.MoveTowards(doorChild.position, finalPosition, Time.deltaTime * doorSpeed);
            yield return null;
        }
        
        isOpened = openStatus;
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Enemy") && !isOpened)
        {
            ONInteractionChange?.Invoke(false);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Enemy") && isOpened)
        {
            var enemy = other.transform.GetComponent<EnemyAI>();
            
            if (enemy.EnemyState == EnemyState.Patrol)
            {
                ONInteractionChange?.Invoke(true);
            }
        }
    }
}
