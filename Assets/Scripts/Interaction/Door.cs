using System.Collections;
using UnityEngine;

namespace Mouth.Interaction
{
    [RequireComponent(typeof(Collider))]
    public class Door : MonoBehaviour, IInteractable
    {
        [Header("Door's Slide Direction")] 
        [SerializeField] private DoorDirection doorDirection;
    
        [Header("Values"), Min(0)]
        [SerializeField] private float doorSlideRange = 2f;
        [SerializeField] private float doorSlideSpeed = 5f;
        [SerializeField] private float playerSlideForce = 5f;

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
        private event InteractionEvent OnInteractionChange;

        private void OnEnable()
        {
            OnInteractionChange += HandleDoorStatusChange;
        }

        private void OnDisable()
        {
            OnInteractionChange -= HandleDoorStatusChange;
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
            
            minDoorSlideValue = Mathf.Min(startPosition.x, finishPosition.x);
            maxDoorSlideValue = Mathf.Max(startPosition.x, finishPosition.x);
        }

        private IEnumerator PlayerDoorInteraction(float normal)
        {
            while (!Input.GetMouseButtonUp(0))
            {
                var doorPosition = transform.localPosition;
                var slideDistance = playerSlideForce * normal * Input.GetAxis("Mouse X");
                var positionAfterSlide = new Vector3(doorPosition.x - slideDistance, doorPosition.y, doorPosition.z);
        
                positionAfterSlide.x = Mathf.Clamp(positionAfterSlide.x, minDoorSlideValue, maxDoorSlideValue);
                transform.localPosition = positionAfterSlide;

                yield return null;
            }
        }

        private void HandleDoorStatusChange(bool isDoorOpened)
        {
            if (activeCoroutine != null)
            {
                StopCoroutine(activeCoroutine);
            }
            
            if (!isDoorOpened)
            {
                activeCoroutine = StartCoroutine(StartDoorMove(finishPosition));
            }
            else
            {
                activeCoroutine = StartCoroutine(StartDoorMove(startPosition));
            }
        }

        private IEnumerator StartDoorMove(Vector3 finalPosition)
        {
            while (transform.localPosition != finalPosition) 
            {
                transform.localPosition = Vector3.MoveTowards(transform.localPosition, finalPosition, doorSlideSpeed * Time.deltaTime);
                yield return null;
            }
        }

        public void InvokeDoorEvent(bool isDoorOpened)
        {
            OnInteractionChange?.Invoke(isDoorOpened);
        }

        public CursorType GetCursorType()
        {
            return CursorType.Door;
        }

        public void HandleInteraction(Interaction interaction)
        {
            if (Input.GetMouseButtonDown(0))
            {
                if (activeCoroutine != null)
                {
                    StopCoroutine(activeCoroutine);
                }

                activeCoroutine = StartCoroutine(PlayerDoorInteraction(interaction.GetHitNormal().z));
            }
        }
    }
}
