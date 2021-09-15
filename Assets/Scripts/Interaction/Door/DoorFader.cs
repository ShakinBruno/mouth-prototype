using System.Collections;
using UnityEngine;

namespace Mouth.Interaction.Door
{
    [RequireComponent(typeof(CanvasGroup))]
    public class DoorFader : MonoBehaviour
    {
        [Header("Values"), Min(0)]
        [SerializeField] private float fadeInTime = 1f;
        [SerializeField] private float fadeOutTime = 2f;
        [SerializeField] private float fadeWaitTime = 0.5f;
        
        private CanvasGroup canvasGroup;
        private Coroutine activeCoroutine;

        private void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }

        private Coroutine Fade(float targetAlpha, float time)
        {
            if (activeCoroutine != null)
            {
                StopCoroutine(activeCoroutine);
            }

            activeCoroutine = StartCoroutine(FadeRoutine(targetAlpha, time));
            
            return activeCoroutine;
        }

        private IEnumerator FadeRoutine(float targetAlpha, float time)
        {
            while (!Mathf.Approximately(canvasGroup.alpha, targetAlpha))
            {
                canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, targetAlpha, Time.deltaTime / time);

                yield return null;
            }
        }

        public Coroutine FadeOut()
        {
            return Fade(1f, fadeOutTime);
        }
        
        public Coroutine FadeIn()
        {
            return Fade(0f, fadeInTime);
        }

        public WaitForSeconds FadeWait()
        {
            return new WaitForSeconds(fadeWaitTime);
        }
    }
}