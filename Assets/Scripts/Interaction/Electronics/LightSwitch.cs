using System.Collections.Generic;
using UnityEngine;

namespace Mouth.Interaction.Electronics
{
    [RequireComponent(typeof(Collider))]
    public class LightSwitch : MonoBehaviour, IInteractable, IElectronics
    {
        [Header("References")]
        [SerializeField] private Light[] lights;
        
        [Header("Light Settings")]
        [SerializeField] private bool isLightActive = true;

        private static readonly int IsActive = Animator.StringToHash("isActive");

        private Fusebox fusebox;

        private delegate void SwitchChangeEvent(bool setActive, bool updateAnimations, bool updateLights);
        private event SwitchChangeEvent OnSwitchChange;

        private void Awake()
        {
            fusebox = FindObjectOfType<Fusebox>();
        }
        
        private void OnEnable()
        {
            OnSwitchChange += SwitchInteraction;
        }

        private void OnDisable()
        {
            OnSwitchChange -= SwitchInteraction;
        }

        private void SwitchInteraction(bool setActive, bool updateAnimations, bool updateLights)
        {
            if (updateAnimations && TryGetComponent<Animator>(out var animator))
            {
                animator.SetBool(IsActive, setActive);
            }
            
            if (updateLights)
            {
                SetLights(setActive);
            }
        }

        private void SetLights(bool setActive)
        {
            foreach (var localLight in lights)
            {
                localLight.enabled = setActive;
            }
        }

        public CursorType GetCursorType()
        {
            return CursorType.LightSwitch;
        }

        public void HandleInteraction(Interaction interaction)
        {
            if (Input.GetMouseButtonDown(0))
            {
                isLightActive = !isLightActive;
                OnSwitchChange?.Invoke(isLightActive, true, fusebox.GetIsActive());

                if (isLightActive && fusebox.GetIsActive())
                {
                    fusebox.CountActiveElectronics();
                }
            }
        }

        public void ChangeStateOfElectronics(bool isFuseboxActive, bool isEmergencyShutdown, bool updateAnimations)
        {
            if (isEmergencyShutdown)
            {
                isLightActive = false;
            }
            
            if (!isFuseboxActive)
            {
                OnSwitchChange?.Invoke(false, updateAnimations, true);
            }
            else
            {
                OnSwitchChange?.Invoke(isLightActive, updateAnimations, true);
            }
        }

        public IEnumerable<object> GetElectronics()
        {
            return lights;
        }
    }
}