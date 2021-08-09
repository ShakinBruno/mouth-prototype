using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class DoorInteraction : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform doorBody;

    [Header("Door's Slide Direction")] 
    [SerializeField] private DoorDirection doorDirection;
    
    [Header("Values"), Min(0)]
    [SerializeField] private float doorSlideRange = 2f;
    [SerializeField] private float doorSlideSpeed = 5f;
    [SerializeField] private float playerSlideForce = 5f;
    
    [HideInInspector] public bool isOpened;

    private float minDoorSlideValue;
    private float maxDoorSlideValue;
    
    private Vector3 startPosition;
    private Vector3 finishPosition;
    private Coroutine activeCoroutine;
    private enum DoorDirection
    {
        Left,
        Right
    }

    private delegate void InteractionEvent(bool isDoorOpened);
    private InteractionEvent onInteractionChange;

    private void Awake()
    {
        onInteractionChange += HandleDoorStatusChange;
    }

    private void Start()
    {
        startPosition = doorBody.localPosition;

        switch (doorDirection)
        {
            case DoorDirection.Left:
                finishPosition = doorBody.localPosition + new Vector3(doorSlideRange, 0f, 0f);
                break;
            case DoorDirection.Right:
                finishPosition = doorBody.localPosition - new Vector3(doorSlideRange, 0f, 0f);
                break;
        }
        
        minDoorSlideValue = Mathf.Min(startPosition.x, finishPosition.x);
        maxDoorSlideValue = Mathf.Max(startPosition.x, finishPosition.x);
    }
    
    public void PlayerDoorInteraction(float direction)
    {
        var doorPosition = doorBody.localPosition;
        var slideDistance = playerSlideForce * direction * Input.GetAxis("Mouse X") * Time.deltaTime;
        var positionAfterSlide = new Vector3(doorPosition.x - slideDistance, doorPosition.y, doorPosition.z);
        
        positionAfterSlide.x = Mathf.Clamp(positionAfterSlide.x, minDoorSlideValue, maxDoorSlideValue);
        doorBody.localPosition = positionAfterSlide;
    }

    private void HandleDoorStatusChange(bool isDoorOpened)
    {
        if (activeCoroutine != null)
        {
            StopCoroutine(activeCoroutine);
        }
            
        if (!isDoorOpened)
        {
            activeCoroutine = StartCoroutine(StartDoorMoving(finishPosition, true));
        }
        else
        {
            activeCoroutine = StartCoroutine(StartDoorMoving(startPosition, false));
        }
    }

    private IEnumerator StartDoorMoving( Vector3 finalPosition, bool isDoorOpened)
    {
        while (doorBody.localPosition != finalPosition) 
        {
            doorBody.localPosition = Vector3.MoveTowards(doorBody.localPosition, finalPosition, Time.deltaTime * doorSlideSpeed);
            yield return null;
        }
        
        isOpened = isDoorOpened;
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Enemy") && !isOpened)
        {
            onInteractionChange?.Invoke(false);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Enemy") && isOpened)
        {
            var enemy = other.transform.GetComponent<EnemyAI>();
            
            if (enemy != null && enemy.EnemyState == EnemyState.Patrol)
            {
                onInteractionChange?.Invoke(true);
            }
        }
    }
}
