using System.Collections;
using UnityEngine;

namespace Mouth.Interaction
{
    [RequireComponent(typeof(CanvasGroup))]
    public class DoorFader : MonoBehaviour
    {
        [Header("Values"), Min(0)]
        [SerializeField] private float fadeInTime = 1f;
        [SerializeField] private float fadeOutTime = 2f;
        [SerializeField] private float fadeWaitTime = 0.5f;
        
        private CanvasGroup canvasGroup;
        private Coroutine currentActiveFade;

        private void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }

        private Coroutine Fade(float targetAlpha, float time)
        {
            if (currentActiveFade != null)
            {
                StopCoroutine(currentActiveFade);
            }

            currentActiveFade = StartCoroutine(FadeRoutine(targetAlpha, time));
            return currentActiveFade;
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