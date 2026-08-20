using UnityEngine;
using UnityEngine.SceneManagement;
using PhobiaReliefTherapy.Therapy;
using PhobiaReliefTherapy.Admin;

namespace PhobiaReliefTherapy.Bootstrap
{
    public static class Phase2SceneBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void Initialize()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            switch (scene.name)
            {
                case "FeedbackScene":
                    if (Object.FindObjectOfType<FeedbackManager>() == null)
                        new GameObject("FeedbackManager").AddComponent<FeedbackManager>();
                    break;
                case "DashboardScene":
                    if (Object.FindObjectOfType<DashboardSessionHistory>() == null)
                        new GameObject("DashboardSessionHistory").AddComponent<DashboardSessionHistory>();
                    break;
                case "AdminScene":
                    if (Object.FindObjectOfType<AdminDashboardManager>() == null)
                        new GameObject("AdminDashboard").AddComponent<AdminDashboardManager>();
                    break;
            }
        }
    }
}
