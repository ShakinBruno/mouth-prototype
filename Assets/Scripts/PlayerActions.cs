using UnityEngine;

public class PlayerActions : MonoBehaviour
{
    [SerializeField] private Color highlightColor;
    [SerializeField] private float interactionDistance;
    [SerializeField] private LayerMask interactionMask;

    private Color originalColor;
    private GameObject lastHighlightedObject;

    private void Update()
    {
        Highlight();
    }

    private void Highlight()
    {
        if (Camera.main is null) return;
        
        var cameraTransform = Camera.main.transform;
        var forward = cameraTransform.TransformDirection(Vector3.forward);

        if (Physics.Raycast(cameraTransform.position, forward, out var hit, interactionDistance, interactionMask, QueryTriggerInteraction.Ignore))
        {
            HighlightObject(hit.collider.gameObject);
            Interaction(hit);
        }
        else
        {
            ClearHighlighted();
        }
    }

    private void Interaction(RaycastHit hit)
    {
        if (!Input.GetMouseButtonDown(0)) return;

        switch (hit.collider.tag)
        {
            case "Door":
                MoveDoor(hit);
                break;
        }
    }

    private void MoveDoor(RaycastHit hit)
    {
        var door = hit.transform.GetComponentInParent<DoorInteraction>();
        
        if(door != null)
        {
            door.ONInteractionChange?.Invoke(door.isOpened);
        }
    }

    private void HighlightObject(GameObject hitObject)
    {
        if (lastHighlightedObject == hitObject) return;
        
        ClearHighlighted();

        if (hitObject.TryGetComponent<MeshRenderer>(out var meshRenderer))
        {
            var material = meshRenderer.material;

            originalColor = material.color;
            material.color = highlightColor;
            lastHighlightedObject = hitObject;
        }
    }

    private void ClearHighlighted()
    {
        if (lastHighlightedObject != null && lastHighlightedObject.TryGetComponent<MeshRenderer>(out var meshRenderer))
        {
            meshRenderer.material.color = originalColor;
            lastHighlightedObject = null;
        }
    }
}
