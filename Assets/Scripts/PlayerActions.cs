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
        var forward = cameraTransform.TransformDirection(Vector3.forward);

        if (Physics.Raycast(cameraTransform.position, forward, out var hit, interactionDistance, interactionMask, QueryTriggerInteraction.Ignore))
        {
            ObjectInteraction(hit);
        }
        else
        {
            ClearHighlighted();
        }
    }

    private void ObjectInteraction(RaycastHit hit)
    {
        switch (hit.collider.tag)
        {
            case "Door":
            {
                InteractWithDoor(hit);
            } 
                break;
        }
    }

    private void InteractWithDoor(RaycastHit hit)
    {
        HighlightTrigger(doorHighlightColor);
        
        if (!Input.GetMouseButton(0)) return;
        
        var door = hit.transform.GetComponentInParent<DoorInteraction>();
        
        if(door != null)
        {
            door.PlayerDoorInteraction(hit.transform.position);
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
