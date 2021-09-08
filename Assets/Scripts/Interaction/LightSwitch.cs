using UnityEngine;

namespace Mouth.Interaction
{
    [RequireComponent(typeof(Collider))]
    public class LightSwitch : MonoBehaviour, IInteractable
    {
        [Header("References")]
        [SerializeField] private Light[] lights;

        private static readonly int IsActive = Animator.StringToHash("isActive");
        private bool isEnabled = true;

        private delegate void SwitchChangeEvent(bool isActive);
        private event SwitchChangeEvent OnSwitchChange;

        private void Awake()
        {
            foreach (var localLight in lights)
            {
                if (localLight.isActiveAndEnabled) continue;
                
                isEnabled = false;
                break;
            }
        }

        private void Start()
        {
            OnSwitchChange?.Invoke(isEnabled);
        }

        private void OnEnable()
        {
            OnSwitchChange += SwitchInteraction;
        }

        private void OnDisable()
        {
            OnSwitchChange -= SwitchInteraction;
        }

        private void SwitchInteraction(bool setActive)
        {
            if (TryGetComponent<Animator>(out var animator))
            {
                animator.SetBool(IsActive, setActive);
            }
            
            foreach (var localLight in lights)
            {
                localLight.enabled = setActive;
            }

            isEnabled = !setActive;
        }

        public CursorType GetCursorType()
        {
            return CursorType.LightSwitch;
        }

        public void HandleInteraction(Interaction interaction)
        {
            if (Input.GetMouseButtonDown(0))
            {
                OnSwitchChange?.Invoke(isEnabled);
            }
        }
    }
}