using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;
using Random = UnityEngine.Random;

namespace Mouth.Interaction.Electronics
{
    public class TV : MonoBehaviour, IInteractable, IElectronics
    {
        [Header("References")]
        [SerializeField] private VideoPlayer videoPlayer;
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private MeshRenderer screenRenderer;
        [SerializeField] private Light[] lights;
        
        [Header("Video Clips")]
        [SerializeField] private VideoClip[] TVChannels;
        
        [Header("TV Settings"), Min(0)]
        [SerializeField] private float channelTransition = 0.5f;
        [SerializeField] private bool isTVActive = true;

        private int currentChannelIndex;
        
        private Fusebox fusebox;
        private Coroutine activeCoroutine;
        
        private delegate void StateChangeEvent(bool setActive, bool shouldUpdateTV);
        private event StateChangeEvent OnStateChange;

        private void Awake()
        {
            fusebox = FindObjectOfType<Fusebox>();
        }
        
        private void OnEnable()
        {
            OnStateChange += HandleInteraction;
        }
        
        private void OnDisable()
        {
            OnStateChange -= HandleInteraction;
        }

        private void Start()
        {
            videoPlayer.clip = TVChannels[currentChannelIndex];
        }

        private void HandleInteraction(bool setActive, bool shouldUpdateTV)
        {
            if (shouldUpdateTV)
            {
                screenRenderer.enabled = setActive;
                audioSource.mute = !setActive;
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

        private IEnumerator SwitchToNextChannel(int channelIndex)
        {
            OnStateChange?.Invoke(false, true);
            videoPlayer.clip = TVChannels[channelIndex];
            videoPlayer.frame = Random.Range(0, (int)videoPlayer.frameCount);

            yield return new WaitForSeconds(channelTransition);
            
            OnStateChange?.Invoke(true, true);
        }
        
        private int GetNextChannelIndex(int index)
        {
            return (index + 1) % TVChannels.Length;
        }
        
        public CursorType GetCursorType()
        {
            return CursorType.TV;
        }

        public void HandleInteraction(Interaction interaction)
        {
            if (Input.GetMouseButtonDown(0))
            {
                isTVActive = !isTVActive;
                OnStateChange?.Invoke(isTVActive, fusebox.GetIsActive());

                if (isTVActive && fusebox.GetIsActive())
                {
                    fusebox.CountActiveElectronics();
                }
            }

            if (Input.GetMouseButtonDown(1) && isTVActive && fusebox.GetIsActive())
            {
                currentChannelIndex = GetNextChannelIndex(currentChannelIndex);
                
                if (activeCoroutine != null)
                {
                    StopCoroutine(activeCoroutine);
                }
                
                activeCoroutine = StartCoroutine(SwitchToNextChannel(currentChannelIndex));
            }
        }

        public void ChangeStateOfElectronics(bool isFuseboxActive, bool isEmergencyShutdown, bool updateAnimations)
        {
            if (isEmergencyShutdown)
            {
                isTVActive = false;
            }

            if (!isFuseboxActive)
            {
                OnStateChange?.Invoke(false, true);
            }
            else
            {
                OnStateChange?.Invoke(isTVActive, true);
            }
        }

        public IEnumerable<object> GetElectronics()
        {
            return lights;
        }
    }
}