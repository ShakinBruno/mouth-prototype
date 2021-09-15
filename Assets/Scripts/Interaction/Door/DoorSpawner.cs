using System;
using System.Collections;
using UnityEngine;

namespace Mouth.Interaction.Door
{
    [RequireComponent(typeof(Collider))]
    public class DoorSpawner : MonoBehaviour, IInteractable
    {
        [Header("References")]
        [SerializeField] private Transform[] spawnPoints;
        
        private CharacterController playerController;
        private DoorFader fader;
        private Coroutine activeCoroutine;

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
            var otherSpawnPoint = GetClosestSpawnPoint().transform;
            
            playerTransform.position = otherSpawnPoint.position;
            playerTransform.rotation = otherSpawnPoint.rotation;

            yield return fader.FadeWait();
            yield return fader.FadeIn();
            
            playerController.enabled = true;
        }
        
        private Transform GetClosestSpawnPoint()
        {
            var distances = new float[spawnPoints.Length];
            
            for (var i = 0; i < spawnPoints.Length; i++)
            {
                distances[i] = Vector3.Distance(playerController.transform.position, spawnPoints[i].position);
            }
            
            Array.Sort(distances, spawnPoints);

            return spawnPoints[spawnPoints.Length - 1];
        }

        public CursorType GetCursorType()
        {
            return CursorType.DoorTeleport;
        }

        public void HandleInteraction(Interaction interaction)
        {
            if (Input.GetMouseButtonDown(0))
            {
                if (activeCoroutine != null)
                {
                    StopCoroutine(activeCoroutine);
                }
                
                activeCoroutine = StartCoroutine(Transition());
            }
        }
    }
}