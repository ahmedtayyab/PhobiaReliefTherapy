using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using PhobiaReliefTherapy.Data;
using PhobiaReliefTherapy.Managers;

namespace PhobiaReliefTherapy.Therapy
{
    /// <summary>
    /// Measures baseline vitals (simulated) before entering the Safe Room.
    /// </summary>
    public class BaselineManager : MonoBehaviour
    {
        [Header("UI Elements")]
        public TextMeshProUGUI instructionText;
        public TextMeshProUGUI timerText;
        public TextMeshProUGUI resultText;
        public TextMeshProUGUI stageText;
        public TextMeshProUGUI sensorModeText;
        public Image progressBarFill;
        public Button continueButton;

        public float measurementDuration = 10f; // seconds

        private void Start()
        {
            AutoBindMissingFields();

            if (continueButton != null)
                continueButton.gameObject.SetActive(false);
            if (resultText != null)
                resultText.text = "";
            if (stageText != null)
                stageText.text = $"Stage {UserData.CurrentStage}: Baseline Measurement";

            SensorManager.EnsureInstanceExists();
            SensorManager.Instance.InitializeSensor();
            VRManager.EnsureInstanceExists();
            VRManager.Instance.InitializeVR();

            if (sensorModeText != null)
                sensorModeText.text = SensorManager.Instance.UseMockSensor ? "Sensor: mock mode" : "Sensor: live mode";

            StartBaselineMeasurement();
        }

        private void AutoBindMissingFields()
        {
            if (instructionText == null)
                instructionText = AutoBindField<TextMeshProUGUI>("InstructionText");
            if (timerText == null)
                timerText = AutoBindField<TextMeshProUGUI>("TimerText");
            if (resultText == null)
                resultText = AutoBindField<TextMeshProUGUI>("ResultText");
            if (stageText == null)
                stageText = AutoBindField<TextMeshProUGUI>("StageText");
            if (sensorModeText == null)
                sensorModeText = AutoBindField<TextMeshProUGUI>("SensorModeText");
            if (progressBarFill == null)
                progressBarFill = AutoBindField<Image>("ProgressBarFill");
            if (continueButton == null)
                continueButton = AutoBindField<Button>("ContinueButton");
        }

        private T AutoBindField<T>(string objectName) where T : Component
        {
            T result = AutoBindHelper.FindComponentInChildrenByName<T>(transform, objectName);
            return result != null ? result : AutoBindHelper.FindComponentByName<T>(objectName);
        }

        private void StartBaselineMeasurement()
        {
            instructionText.text = "Please relax. Measuring baseline heart rate...";
            SensorManager.Instance.StartBaselineMeasurement(measurementDuration, OnBaselineComplete);
            StartCoroutine(BaselineTimerRoutine());
        }

        private IEnumerator BaselineTimerRoutine()
        {
            float timer = measurementDuration;
            while (timer > 0)
            {
                float progress = 1f - (timer / measurementDuration);
                if (timerText != null)
                    timerText.text = Mathf.CeilToInt(timer).ToString() + "s";
                if (progressBarFill != null)
                    progressBarFill.fillAmount = Mathf.Clamp01(progress);

                yield return new WaitForSeconds(1f);
                timer -= 1f;
            }

            if (timerText != null)
                timerText.text = "0s";
            if (progressBarFill != null)
                progressBarFill.fillAmount = 1f;
        }

        private void OnBaselineComplete(int heartRate)
        {
            UserData.BaselineHeartRate = heartRate;
            if (resultText != null)
                resultText.text = $"Baseline Heart Rate: {heartRate} BPM";
            if (instructionText != null)
                instructionText.text = "Baseline measurement complete. You may proceed to the safe room.";

            if (continueButton != null)
            {
                continueButton.gameObject.SetActive(true);
                continueButton.onClick.RemoveAllListeners();
                continueButton.onClick.AddListener(() => {
                    SceneLoader.Instance.LoadScene("SafeRoomScene");
                });
            }
        }
    }
}
