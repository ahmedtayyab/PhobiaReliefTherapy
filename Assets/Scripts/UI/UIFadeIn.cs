using System.Collections;
using UnityEngine;

namespace PhobiaReliefTherapy.UI
{
    [RequireComponent(typeof(CanvasGroup))]
    public class UIFadeIn : MonoBehaviour
    {
        public float delay = 0f;
        public float duration = 0.6f;
        public bool scaleUp = false;
        public float initialScale = 0.95f;

        private CanvasGroup canvasGroup;

        private void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
            canvasGroup.alpha = 0f;
            if (scaleUp)
            {
                transform.localScale = Vector3.one * initialScale;
            }
        }

        private void Start()
        {
            StartCoroutine(Animate());
        }

        private IEnumerator Animate()
        {
            if (delay > 0)
            {
                yield return new WaitForSeconds(delay);
            }

            float elapsed = 0f;
            Vector3 startScale = transform.localScale;
            Vector3 endScale = Vector3.one;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                
                // Smooth step easing function
                t = t * t * (3f - 2f * t);

                if (canvasGroup != null)
                {
                    canvasGroup.alpha = t;
                }

                if (scaleUp)
                {
                    transform.localScale = Vector3.Lerp(startScale, endScale, t);
                }
                yield return null;
            }

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
            }

            if (scaleUp)
            {
                transform.localScale = endScale;
            }
        }
    }
}
