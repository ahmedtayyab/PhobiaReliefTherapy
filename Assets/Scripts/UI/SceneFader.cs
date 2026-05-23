using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace PhobiaReliefTherapy.UI
{
    [RequireComponent(typeof(Canvas))]
    public class SceneFader : MonoBehaviour
    {
        public static SceneFader Instance { get; private set; }

        [Tooltip("Black overlay image used for fade transitions")]
        public Image fadeImage;

        [Tooltip("Time in seconds for fade in/out transitions")]
        public float fadeDuration = 0.4f;

        private bool isFading;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (fadeImage != null)
            {
                fadeImage.material = null;
                fadeImage.color = new Color(0f, 0f, 0f, 1f);
            }
        }

        private void Start()
        {
            if (fadeImage != null)
                StartCoroutine(FadeRoutine(1f, 0f));
        }

        public void FadeToScene(string sceneName)
        {
            if (isFading || string.IsNullOrEmpty(sceneName))
                return;

            StartCoroutine(FadeAndLoad(sceneName));
        }

        private IEnumerator FadeAndLoad(string sceneName)
        {
            isFading = true;
            yield return FadeRoutine(0f, 1f);
            yield return SceneManager.LoadSceneAsync(sceneName);
            yield return null;
            yield return FadeRoutine(1f, 0f);
            isFading = false;
        }

        private IEnumerator FadeRoutine(float fromAlpha, float toAlpha)
        {
            if (fadeImage == null)
                yield break;

            float elapsed = 0f;
            Color color = fadeImage.color;
            color.a = fromAlpha;
            fadeImage.color = color;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / fadeDuration);
                color.a = Mathf.Lerp(fromAlpha, toAlpha, t);
                fadeImage.color = color;
                yield return null;
            }

            color.a = toAlpha;
            fadeImage.color = color;
        }
    }
}
