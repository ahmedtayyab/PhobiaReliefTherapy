using UnityEngine;
using UnityEngine.SceneManagement;

namespace PhobiaReliefTherapy.Managers
{
    /// <summary>
    /// Handles loading of different scenes throughout the application.
    /// This is a simple wrapper around Unity's SceneManager.
    /// </summary>
    public class SceneLoader : MonoBehaviour
    {
        public static SceneLoader Instance { get; private set; }

        private void Awake()
        {
            // Simple Singleton pattern
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// Loads a scene by its exact name.
        /// Ensure the scene is added to File -> Build Settings.
        /// </summary>
        public void LoadScene(string sceneName)
        {
            Debug.Log($"Loading Scene: {sceneName}");
            SceneManager.LoadScene(sceneName);
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