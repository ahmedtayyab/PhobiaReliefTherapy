using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace PhobiaReliefTherapy.Managers
{
    /// <summary>
    /// Handles loading of different scenes throughout the application with smooth transitions.
    /// </summary>
    public class SceneLoader : MonoBehaviour
    {
        private static SceneLoader _instance;
        public static SceneLoader Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<SceneLoader>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("SceneLoader", typeof(SceneLoader));
                        _instance = go.GetComponent<SceneLoader>();
                    }
                }
                return _instance;
            }
        }
        private CanvasGroup fadeGroup;

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
                CreateFadeCanvas();
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }

        private void CreateFadeCanvas()
        {
            // Create a canvas specifically for screen transitions
            var canvasGO = new GameObject("TransitionCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGO.transform.SetParent(this.transform, false);
            
            var canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 9999; // Render on top of everything
            
            var scaler = canvasGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            
            var overlayGO = new GameObject("FadeImage", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup));
            overlayGO.transform.SetParent(canvasGO.transform, false);
            
            var rect = overlayGO.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            
            var image = overlayGO.GetComponent<Image>();
            image.color = new Color32(13, 14, 18, 255); // #0D0E12 charcoal theme background
            
            fadeGroup = overlayGO.GetComponent<CanvasGroup>();
            fadeGroup.alpha = 0f;
            fadeGroup.blocksRaycasts = false;
        }

        /// <summary>
        /// Loads a scene by its exact name with a smooth transition.
        /// Ensure the scene is added to File -> Build Settings.
        /// </summary>
        public void LoadScene(string sceneName)
        {
            Debug.Log($"Transitioning to Scene: {sceneName}");
            StartCoroutine(TransitionCoroutine(sceneName));
        }

        private System.Collections.IEnumerator TransitionCoroutine(string sceneName)
        {
            if (fadeGroup != null)
            {
                fadeGroup.blocksRaycasts = true;
                float elapsed = 0f;
                float duration = 0.35f;
                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    fadeGroup.alpha = Mathf.Clamp01(elapsed / duration);
                    yield return null;
                }
                fadeGroup.alpha = 1f;
            }

            // Async scene load
            var op = SceneManager.LoadSceneAsync(sceneName);
            while (!op.isDone)
            {
                yield return null;
            }

            // Give a tiny frame delay for Awake/Start methods of new scene components to execute
            yield return new WaitForSeconds(0.1f);

            if (fadeGroup != null)
            {
                float elapsed = 0f;
                float duration = 0.35f;
                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    fadeGroup.alpha = Mathf.Clamp01(1f - (elapsed / duration));
                    yield return null;
                }
                fadeGroup.alpha = 0f;
                fadeGroup.blocksRaycasts = false;
            }
        }

        /// <summary>
        /// Reloads the currently active scene. Useful for restarting a therapy session.
        /// </summary>
        public void ReloadCurrentScene()
        {
            string currentScene = SceneManager.GetActiveScene().name;
            LoadScene(currentScene);
        }

        /// <summary>
        /// Quits the application.
        /// </summary>
        public void QuitApplication()
        {
            Debug.Log("Quitting Application...");
            Application.Quit();
        }
    }
}