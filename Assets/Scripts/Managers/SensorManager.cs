using System;
using System.Collections;
using UnityEngine;

namespace PhobiaReliefTherapy.Managers
{
    /// <summary>
    /// Abstracts sensor input so the app can be tested without Polar H10 or other hardware.
    /// Replace or extend this class later with real Bluetooth/Polar H10 code.
    /// </summary>
    public class SensorManager : MonoBehaviour
    {
        public static SensorManager Instance { get; private set; }

        public bool IsSensorConnected { get; private set; }
        public bool UseMockSensor { get; private set; } = true;
        public int CurrentHeartRate { get; private set; } = 0;

        private Coroutine baselineRoutine;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeSensor();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public void InitializeSensor()
        {
            IsSensorConnected = false;
            UseMockSensor = true;
            CurrentHeartRate = 0;
            Debug.Log("SensorManager: Using mock sensor input for development.");
        }

        public void StartBaselineMeasurement(float durationSeconds, Action<int> onComplete)
        {
            if (baselineRoutine != null)
            {
                StopCoroutine(baselineRoutine);
                baselineRoutine = null;
            }

            baselineRoutine = StartCoroutine(BaselineRoutine(durationSeconds, onComplete));
        }

        private IEnumerator BaselineRoutine(float durationSeconds, Action<int> onComplete)
        {
            float timer = Mathf.Max(1f, durationSeconds);
            int simulatedRate = UnityEngine.Random.Range(70, 91);

            while (timer > 0)
            {
                CurrentHeartRate = simulatedRate;
                yield return new WaitForSeconds(1f);
                timer -= 1f;
            }

            CurrentHeartRate = simulatedRate;
            onComplete?.Invoke(CurrentHeartRate);
            baselineRoutine = null;
        }

        public void StartSessionMonitoring()
        {
            if (UseMockSensor)
            {
                Debug.Log("SensorManager: Starting mock sensor session monitoring.");
                CurrentHeartRate = UnityEngine.Random.Range(70, 95);
            }
            else
            {
                Debug.Log("SensorManager: Starting real sensor session monitoring.");
                // TODO: start real sensor updates here.
            }
        }

        public void StopSessionMonitoring()
        {
            Debug.Log("SensorManager: Stopping sensor session monitoring.");
        }

        public static void EnsureInstanceExists()
        {
            if (Instance == null)
            {
                GameObject go = new GameObject("SensorManager");
                Instance = go.AddComponent<SensorManager>();
            }
        }
    }
}
