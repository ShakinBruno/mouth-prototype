using System;
using UnityEngine;
using UnityEngine.UI;

public class PlayerActions : MonoBehaviour
{
    [Header("References")] 
    [SerializeField] private Image trigger;

    [Header("Mappings")] 
    [SerializeField] private TriggerMapping[] triggerMappings;
    
    [Header("Interaction Mask")] 
    [SerializeField] private LayerMask interactionMask;

    [Header("Values"), Min(0)] 
    [SerializeField] private float interactionDistance;

    private RaycastHit hit;

    [Serializable] private struct TriggerMapping
    {
        public TriggerType type;
        public Color color;
    }

    private void Update()
    {
        InteractWithComponent();
    }

    private void InteractWithComponent()
    {
        if (Camera.main is null) return;

        var cameraTransform = Camera.main.transform;
        var forward = cameraTransform.TransformDirection(Vector3.forward).normalized;

        if (Physics.Raycast(cameraTransform.position, forward, out hit, interactionDistance, interactionMask, QueryTriggerInteraction.Ignore))
        {
            var interactable = hit.transform.GetComponent<IInteractable>();
            
            if(interactable == null) return;
            
            SetTrigger(interactable.GetTriggerType());
            interactable.HandleInteraction(this);
        }
        else
        {
            SetTrigger(TriggerType.Default);
        }
    }

    private void SetTrigger(TriggerType type)
    {
        var mapping = GetTriggerMapping(type);
        
        trigger.color = mapping.color;
    }

    private TriggerMapping GetTriggerMapping(TriggerType type)
    {
        foreach (var mapping in triggerMappings)
        {
            if (mapping.type == type)
            {
                return mapping;
            }
        }

        return triggerMappings[0];
    }

    public RaycastHit GetHit()
    {
        return hit;
    }
}
