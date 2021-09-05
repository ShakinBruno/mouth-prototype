using System;
using System.Collections;
using UnityEngine;

namespace Mouth.Interaction
{
    [RequireComponent(typeof(Collider))]
    public class DoorTeleport : MonoBehaviour, IInteractable
    {
        [Header("Array Of Teleports")]
        [SerializeField] private Transform[] teleports;
        
        private CharacterController playerController;
        private DoorFader fader;

        private void Awake()
        {
            playerController = GameObject.FindWithTag("Player").GetComponent<CharacterController>();
            fader = FindObjectOfType<DoorFader>();
        }

        private IEnumerator Transition()
        {
            playerController.enabled = false;

            yield return fader.FadeOut();

            var playerTransform = playerController.transform;
            var otherPortalTransform = GetClosestSpawnPoint().transform;
            
            playerTransform.position = otherPortalTransform.position;
            playerTransform.rotation = otherPortalTransform.rotation;

            yield return fader.FadeWait();
            yield return fader.FadeIn();
            
            playerController.enabled = true;
        }
        
        private Transform GetClosestSpawnPoint()
        {
            var distances = new float[teleports.Length];
            
            for (var i = 0; i < teleports.Length; i++)
            {
                distances[i] = Vector3.Distance(playerController.transform.position, teleports[i].position);
            }
            
            Array.Sort(distances, teleports);

            return teleports[teleports.Length - 1];
        }

        public TriggerType GetTriggerType()
        {
            return TriggerType.DoorTeleport;
        }

        public void HandleInteraction(Interaction interaction)
        {
            if (Input.GetMouseButtonDown(0))
            {
                StartCoroutine(Transition());
            }
        }
    }
}