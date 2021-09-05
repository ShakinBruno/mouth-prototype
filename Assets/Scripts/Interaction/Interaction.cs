using System;
using UnityEngine;
using UnityEngine.UI;

namespace Mouth.Interaction
{
    [RequireComponent(typeof(CharacterController))]
    public class Interaction : MonoBehaviour
    {
        [Header("References")] 
        [SerializeField] private Camera mainCamera;
        [SerializeField] private Image trigger;

        [Header("Mappings")] 
        [SerializeField] private TriggerMapping[] triggerMappings;

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
            var cameraTransform = mainCamera.transform;
            var forward = cameraTransform.TransformDirection(Vector3.forward);

            if (Physics.Raycast(cameraTransform.position, forward, out hit, interactionDistance, Physics.AllLayers, QueryTriggerInteraction.Ignore))
            {
                if (hit.transform.TryGetComponent<IInteractable>(out var interactable))
                {
                    SetTrigger(interactable.GetTriggerType());
                    interactable.HandleInteraction(this);
                }
                else
                {
                    SetDefaultTrigger();
                }
            }
            else
            {
                SetDefaultTrigger();
            }
        }

        private void SetTrigger(TriggerType type)
        {
            var mapping = GetTriggerMapping(type);
        
            trigger.color = mapping.color;
        }

        private void SetDefaultTrigger()
        {
            trigger.color = triggerMappings[0].color;
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

        public Vector3 GetHitNormal()
        {
            return hit.transform.InverseTransformDirection(hit.normal);
        }
    }
}
