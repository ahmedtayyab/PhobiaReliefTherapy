using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PhobiaReliefTherapy.Data;
using PhobiaReliefTherapy.Managers;

namespace PhobiaReliefTherapy.Therapy
{
    /// <summary>
    /// Manages the Safe Room environment and transitioning to the correct exposure scene.
    /// </summary>
    public class SafeRoomManager : MonoBehaviour
    {
        [Header("UI Elements")]
        public TextMeshProUGUI welcomeText;
        public TextMeshProUGUI infoText;
        public TextMeshProUGUI statusText;
        public Button startExposureButton;
        public Button backToDashboardButton;

        private void Start()
        {
            AutoBindMissingFields();

            VRManager.EnsureInstanceExists();
            SensorManager.EnsureInstanceExists();

            if (welcomeText != null)
                welcomeText.text = $"Welcome, {UserData.Username}.";

            if (infoText != null)
                infoText.text = $"Selected phobia: {UserData.SelectedPhobia}\nBaseline HR: {UserData.BaselineHeartRate} BPM\nDifficulty: {UserData.SelectedDifficulty}";

            if (statusText != null)
                statusText.text = "Preparing your safe room...";

            VRManager.Instance.InitializeVR();
            SensorManager.Instance.StartSessionMonitoring();

            if (statusText != null)
                statusText.text = SensorManager.Instance.UseMockSensor ? "Sensor mode: mock" : "Sensor mode: live";

            if (startExposureButton != null)
            {
                startExposureButton.onClick.RemoveAllListeners();
                startExposureButton.onClick.AddListener(StartExposure);
            }

            if (backToDashboardButton != null)
            {
                backToDashboardButton.onClick.RemoveAllListeners();
                backToDashboardButton.onClick.AddListener(() => SceneLoader.Instance.LoadScene("DashboardScene"));
            }
        }

        private void AutoBindMissingFields()
        {
            if (welcomeText == null)
                welcomeText = AutoBindField<TextMeshProUGUI>("WelcomeText");
            if (infoText == null)
                infoText = AutoBindField<TextMeshProUGUI>("InfoText");
            if (statusText == null)
                statusText = AutoBindField<TextMeshProUGUI>("StatusText");
            if (startExposureButton == null)
                startExposureButton = AutoBindField<Button>("StartExposureButton");
            if (backToDashboardButton == null)
                backToDashboardButton = AutoBindField<Button>("BackToDashboardButton");
        }

        private T AutoBindField<T>(string objectName) where T : Component
        {
            T result = AutoBindHelper.FindComponentInChildrenByName<T>(transform, objectName);
            return result != null ? result : AutoBindHelper.FindComponentByName<T>(objectName);
        }

        private void StartExposure()
        {
            Debug.Log($"Starting Exposure for: {UserData.SelectedPhobia}");
            
            switch (UserData.SelectedPhobia)
            {
                case "Height":
                    SceneLoader.Instance.LoadScene("HeightScene");
                    break;
                case "Darkness":
                    SceneLoader.Instance.LoadScene("DarknessScene");
                    break;
                case "Crowd":
                    SceneLoader.Instance.LoadScene("CrowdScene");
                    break;
                default:
                    Debug.LogWarning("No phobia selected! Returning to Dashboard.");
                    SceneLoader.Instance.LoadScene("DashboardScene");
                    break;
            }
        }
    }
}
