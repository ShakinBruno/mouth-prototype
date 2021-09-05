using UnityEngine;

namespace Mouth.Interaction
{
    [RequireComponent(typeof(Collider))]
    public class LightSwitch : MonoBehaviour, IInteractable
    {
        [Header("References")]
        [SerializeField] private Light[] lights;

        private static readonly int IsActive = Animator.StringToHash("isActive");
        private bool isLightActive = true;

        private delegate void SwitchChangeEvent(bool isActive);
        private event SwitchChangeEvent OnSwitchChange;

        private void Awake()
        {
            foreach (var localLight in lights)
            {
                if (localLight.isActiveAndEnabled) continue;
                
                isLightActive = false;
                break;
            }
        }

        private void Start()
        {
            OnSwitchChange?.Invoke(isLightActive);
        }

        private void OnEnable()
        {
            OnSwitchChange += SwitchInteraction;
        }

        private void OnDisable()
        {
            OnSwitchChange -= SwitchInteraction;
        }

        private void SwitchInteraction(bool isActive)
        {
            if (TryGetComponent<Animator>(out var animator))
            {
                animator.SetBool(IsActive, isActive);
            }
            
            foreach (var localLight in lights)
            {
                localLight.enabled = isActive;
            }

            isLightActive = !isLightActive;
        }

        public TriggerType GetTriggerType()
        {
            return TriggerType.LightSwitch;
        }

        public void HandleInteraction(Interaction interaction)
        {
            if (Input.GetMouseButtonDown(0))
            {
                OnSwitchChange?.Invoke(isLightActive);
            }
        }
    }
}