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
        public Button startExposureButton;

        private void Start()
        {
            welcomeText.text = $"Welcome, {UserData.Username}.";
            infoText.text = $"You have selected {UserData.SelectedPhobia} Therapy.\nTake a deep breath and relax. Your baseline HR is {UserData.BaselineHeartRate} BPM.";

            if (startExposureButton != null)
            {
                startExposureButton.onClick.AddListener(StartExposure);
            }
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
