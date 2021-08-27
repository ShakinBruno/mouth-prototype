using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Door : MonoBehaviour, IInteractable
{
    [Header("Door's Slide Direction")] 
    [SerializeField] private DoorDirection doorDirection;
    
    [Header("Values"), Min(0)]
    [SerializeField] private float doorSlideRange = 2f;
    [SerializeField] private float doorSlideSpeed = 5f;
    [SerializeField] private float playerSlideForce = 5f;

    private Vector3 startPosition;
    private Vector3 finishPosition;
    private Coroutine activeCoroutine;
    private enum DoorDirection
    {
        Left,
        Right
    }

    private delegate void InteractionEvent(bool isDoorOpened);
    private event InteractionEvent ONInteractionChange;

    private void OnEnable()
    {
        ONInteractionChange += HandleDoorStatusChange;
    }

    private void OnDisable()
    {
        ONInteractionChange -= HandleDoorStatusChange;
    }

    private void Start()
    {
        startPosition = transform.localPosition;

        switch (doorDirection)
        {
            case DoorDirection.Left:
                finishPosition = transform.localPosition + new Vector3(doorSlideRange, 0f, 0f);
                break;
            case DoorDirection.Right:
                finishPosition = transform.localPosition - new Vector3(doorSlideRange, 0f, 0f);
                break;
        }
    }

    private void PlayerDoorInteraction(float direction)
    {
        var doorPosition = transform.localPosition;
        var minDoorSlideValue = Mathf.Min(startPosition.x, finishPosition.x);
        var maxDoorSlideValue = Mathf.Max(startPosition.x, finishPosition.x);
        
        var slideDistance = playerSlideForce * direction * Input.GetAxis("Mouse X") * Time.deltaTime;
        var positionAfterSlide = new Vector3(doorPosition.x - slideDistance, doorPosition.y, doorPosition.z);
        
        positionAfterSlide.x = Mathf.Clamp(positionAfterSlide.x, minDoorSlideValue, maxDoorSlideValue);
        transform.localPosition = positionAfterSlide;
    }

    private void HandleDoorStatusChange(bool isDoorOpened)
    {
        if (activeCoroutine != null)
        {
            StopCoroutine(activeCoroutine);
        }
            
        if (!isDoorOpened)
        {
            activeCoroutine = StartCoroutine(StartDoorMoving(finishPosition));
        }
        else
        {
            activeCoroutine = StartCoroutine(StartDoorMoving(startPosition));
        }
    }

    private IEnumerator StartDoorMoving(Vector3 finalPosition)
    {
        while (transform.localPosition != finalPosition) 
        {
            transform.localPosition = Vector3.MoveTowards(transform.localPosition, finalPosition, doorSlideSpeed * Time.deltaTime);
            yield return null;
        }
    }

    public void InvokeDoorEvent(bool isDoorOpened)
    {
        ONInteractionChange?.Invoke(isDoorOpened);
    }

    public TriggerType GetTriggerType()
    {
        return TriggerType.Door;
    }

    public void HandleInteraction(PlayerActions playerActions)
    {
        if (Input.GetAxis("Fire1") != 0f)
        {
            PlayerDoorInteraction(playerActions.GetHit().normal.z);
        }
    }
}
