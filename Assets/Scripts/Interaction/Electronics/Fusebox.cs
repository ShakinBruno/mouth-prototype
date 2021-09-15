using System.Collections;
using System.Linq;
using UnityEngine;

namespace Mouth.Interaction.Electronics
{
    [RequireComponent(typeof(Collider))]
    public class Fusebox : MonoBehaviour, IInteractable
    {
        [Header("Fusebox Settings"), Min(0)] 
        [SerializeField] private int maxElectronicsAmount = 4;
        [SerializeField] private float emergencyShutdownDelay = 0.5f;
        [SerializeField] private bool isFuseboxActive = true;

        private IElectronics[] electronics;
        private Coroutine activeCoroutine;

        private delegate void StateChangeEvent(bool fuseboxState, bool isEmergencyShutdown, bool updateAnimations);

        private event StateChangeEvent OnFuseboxChange;

        private void Awake()
        {
            electronics = FindObjectsOfType<MonoBehaviour>().OfType<IElectronics>().ToArray();
        }

        private void OnEnable()
        {
            OnFuseboxChange += HandleFuseboxChange;
        }

        private void OnDisable()
        {
            OnFuseboxChange -= HandleFuseboxChange;
        }

        private void Start()
        {
            OnFuseboxChange?.Invoke(isFuseboxActive, !isFuseboxActive, true);
        }

        private void HandleFuseboxChange(bool fuseboxState, bool isEmergencyShutdown, bool updateAnimations)
        {
            foreach (var electronic in electronics)
            {
                electronic.ChangeStateOfElectronics(fuseboxState, isEmergencyShutdown, updateAnimations);
            }
        }

        public void CountActiveElectronics()
        {
            var activeElectronicsCount = 0;

            foreach (var electronic in electronics)
            {
                if (electronic.GetElectronics() is Light[] lights)
                {
                    activeElectronicsCount += lights.Count(device => device.enabled);
                }
            }

            CheckForEmergencyShutdown(activeElectronicsCount);
        }

        private void CheckForEmergencyShutdown(int electronicsCount)
        {
            if (electronicsCount > maxElectronicsAmount)
            {
                if (activeCoroutine != null)
                {
                    StopCoroutine(activeCoroutine);
                }

                activeCoroutine = StartCoroutine(EmergencyShutdown());
            }
        }

        private IEnumerator EmergencyShutdown()
        {
            yield return new WaitForSeconds(emergencyShutdownDelay);

            isFuseboxActive = false;
            OnFuseboxChange?.Invoke(isFuseboxActive, true, true);
        }

        public bool GetIsActive()
        {
            return isFuseboxActive;
        }

        public CursorType GetCursorType()
        {
            return CursorType.Fusebox;
        }

        public void HandleInteraction(Interaction interaction)
        {
            if (Input.GetMouseButtonDown(0))
            {
                isFuseboxActive = !isFuseboxActive;
                OnFuseboxChange?.Invoke(isFuseboxActive, false, false);

                if (isFuseboxActive)
                {
                    CountActiveElectronics();
                }
            }
        }
    }
}
