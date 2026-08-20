using System;
using System.Collections;
using UnityEngine;

namespace PhobiaReliefTherapy.Managers
{
    /// <summary>
    /// Abstracts sensor input. Mock fallback per SRS UC-08 / FYP Q9 when Polar H10 is unavailable.
    /// </summary>
    public class SensorManager : MonoBehaviour
    {
        public static SensorManager Instance { get; private set; }

        public bool IsSensorConnected { get; private set; }
        public bool UseMockSensor { get; private set; } = true;
        public int CurrentHeartRate { get; private set; } = 0;

        private Coroutine baselineRoutine;
        private Coroutine exposureRoutine;
        private int mockBaselineHeartRate = 75;
        private float exposureStressFactor = 1f;

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
            mockBaselineHeartRate = UnityEngine.Random.Range(70, 91);

            while (timer > 0)
            {
                CurrentHeartRate = mockBaselineHeartRate + UnityEngine.Random.Range(-2, 3);
                yield return new WaitForSeconds(1f);
                timer -= 1f;
            }

            CurrentHeartRate = mockBaselineHeartRate;
            onComplete?.Invoke(CurrentHeartRate);
            baselineRoutine = null;
        }

        public void StartSessionMonitoring()
        {
            StartExposureMonitoring();
        }

        public void StartExposureMonitoring()
        {
            if (exposureRoutine != null)
            {
                StopCoroutine(exposureRoutine);
                exposureRoutine = null;
            }

            if (UseMockSensor)
            {
                Debug.Log("SensorManager: Starting mock exposure monitoring.");
                exposureStressFactor = 1f;
                exposureRoutine = StartCoroutine(ExposureMockRoutine());
            }
            else
            {
                Debug.Log("SensorManager: Starting real sensor session monitoring.");
                // TODO: Polar H10 BLE read loop (SRS §3.2.4).
            }
        }

        private IEnumerator ExposureMockRoutine()
        {
            int baseRate = mockBaselineHeartRate > 0 ? mockBaselineHeartRate : UnityEngine.Random.Range(70, 90);

            while (true)
            {
                exposureStressFactor = Mathf.Min(exposureStressFactor + 0.02f, 1.25f);
                int stressedRate = Mathf.RoundToInt(baseRate * exposureStressFactor);
                CurrentHeartRate = stressedRate + UnityEngine.Random.Range(-2, 3);
                yield return new WaitForSeconds(1f);
            }
        }

        public void StopSessionMonitoring()
        {
            if (exposureRoutine != null)
            {
                StopCoroutine(exposureRoutine);
                exposureRoutine = null;
            }

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
