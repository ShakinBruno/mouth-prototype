using UnityEngine;
using UnityEngine.UI;

public class PlayerActions : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Image trigger;
    
    [Header("Highlight Colors")]
    [SerializeField] private Color doorHighlightColor;
    
    [Header("Interaction Mask")]
    [SerializeField] private LayerMask interactionMask;
    
    [Header("Values"), Min(0)]
    [SerializeField] private float interactionDistance;
    
    private Color originalColor;

    private void Start()
    {
        originalColor = trigger.color;
    }

    private void Update()
    {
        IsObjectInteractable();
    }

    private void IsObjectInteractable()
    {
        if (Camera.main is null) return;
        
        var cameraTransform = Camera.main.transform;
        var forward = cameraTransform.TransformDirection(Vector3.forward).normalized;

        if (Physics.Raycast(cameraTransform.position, forward, out var hit, interactionDistance, interactionMask, QueryTriggerInteraction.Ignore))
        {
            switch (hit.collider.tag)
            {
                case "Door":
                {
                    InteractWithDoor(hit, hit.normal.z);
                } 
                    break;
            }
        }
        else
        {
            ClearHighlighted();
        }
    }

    private void InteractWithDoor(RaycastHit hit, float direction)
    {
        HighlightTrigger(doorHighlightColor);
        
        if (Input.GetAxis("Fire1") == 0f) return;
        
        var door = hit.transform.GetComponentInParent<DoorInteraction>();
        
        if(door != null)
        {
            door.PlayerDoorInteraction(direction);
        }
    }

    private void HighlightTrigger(Color highlightColor)
    {
        trigger.color = highlightColor;
    }

    private void ClearHighlighted()
    {
        trigger.color = originalColor;
    }
}
