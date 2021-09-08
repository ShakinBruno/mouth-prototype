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
        [SerializeField] private Image cursor;

        [Header("Mappings")] 
        [SerializeField] private CursorMapping[] cursorMappings;

        [Header("Values"), Min(0)] 
        [SerializeField] private float interactionDistance;

        private RaycastHit hit;

        [Serializable] private struct CursorMapping
        {
            public CursorType type;
            public Sprite cursor;
            public Vector2 scale;
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
                    SetCursor(interactable.GetCursorType());
                    interactable.HandleInteraction(this);
                }
                else
                {
                    SetDefaultCursor();
                }
            }
            else
            {
                SetDefaultCursor();
            }
        }

        private void SetCursor(CursorType type)
        {
            var mapping = GetCursorMapping(type);
        
            cursor.sprite = mapping.cursor;
            cursor.rectTransform.localScale = mapping.scale;
        }

        private void SetDefaultCursor()
        {
            cursor.sprite = cursorMappings[0].cursor;
            cursor.rectTransform.localScale = cursorMappings[0].scale;
        }

        private CursorMapping GetCursorMapping(CursorType type)
        {
            foreach (var mapping in cursorMappings)
            {
                if (mapping.type == type)
                {
                    return mapping;
                }
            }

            return cursorMappings[0];
        }

        public Vector3 GetHitNormal()
        {
            return hit.transform.InverseTransformDirection(hit.normal);
        }
    }
}
