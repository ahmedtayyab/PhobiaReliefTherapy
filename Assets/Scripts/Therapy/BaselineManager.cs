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
        public Button continueButton;

        public float measurementDuration = 10f; // seconds

        private void Start()
        {
            continueButton.gameObject.SetActive(false);
            resultText.text = "";
            StartCoroutine(MeasurementRoutine());
        }

        private IEnumerator MeasurementRoutine()
        {
            instructionText.text = "Please relax. Measuring baseline heart rate...";
            
            float timer = measurementDuration;
            while (timer > 0)
            {
                timerText.text = Mathf.CeilToInt(timer).ToString() + "s";
                yield return new WaitForSeconds(1f);
                timer--;
            }

            timerText.text = "0s";
            
            // Simulate Heart Rate between 70 and 90 BPM
            int simulatedHR = Random.Range(70, 91);
            UserData.BaselineHeartRate = simulatedHR;

            resultText.text = $"Baseline Heart Rate: {simulatedHR} BPM";
            instructionText.text = "Measurement Complete.";
            
            continueButton.gameObject.SetActive(true);
            continueButton.onClick.AddListener(() => {
                SceneLoader.Instance.LoadScene("SafeRoomScene");
            });
        }
    }
}
