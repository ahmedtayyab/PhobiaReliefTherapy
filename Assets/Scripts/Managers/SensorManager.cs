using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using PhobiaReliefTherapy.Data;
using PhobiaReliefTherapy.Managers.HeartRate;

namespace PhobiaReliefTherapy.Managers
{
    /// <summary>
    /// Abstracts Polar H10/H9 heart rate input with mock fallback (SRS UC-08).
    /// </summary>
    public class SensorManager : MonoBehaviour
    {
        public static SensorManager Instance { get; private set; }

        [Header("Sensor Preferences")]
        [Tooltip("Try real Polar sensor before mock data.")]
        public bool preferRealSensor = true;

        [Tooltip("Force mock sensor even if Polar is available.")]
        public bool forceMockSensor = false;

        [Tooltip("Receive live BPM from tools/polar_hr_bridge.py over localhost UDP.")]
        public bool enableUdpBridge = true;

        public int udpListenPort = HeartRateBleConstants.DefaultUdpPort;

        [Header("Debug")]
        public bool showDebugOverlay = true;

        public bool IsSensorConnected => activeProvider != null && activeProvider.IsConnected && !UseMockSensor;
        public bool IsProviderReady => activeProvider != null;
        public bool UseMockSensor { get; private set; } = true;
        public bool LiveRequested { get; private set; } = true;
        public int CurrentHeartRate
        {
            get
            {
                int raw = activeProvider?.LatestHeartRate ?? 0;
                return HeartRateBleConstants.ClampDeviceBpm(raw);
            }
        }
        public string SensorSourceName => activeProvider?.SourceName ?? "None";
        public string SensorStatusMessage => activeProvider?.StatusMessage ?? "Sensor not initialized.";

        private IHeartRateProvider activeProvider;
        private MockHeartRateProvider mockProvider;
        private UdpHeartRateProvider udpProvider;

        private Coroutine baselineRoutine;
        private Coroutine exposureRoutine;
        private Coroutine initializeRoutine;
        private bool isInitialized;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
                return;
            }

#if !UNITY_EDITOR
            showDebugOverlay = false;
#endif
        }

        private void Update()
        {
            udpProvider?.Tick();
        }

        public void InitializeSensor()
        {
            StartInitialize(null, tryLive: LiveRequested);
        }

        public void ReconnectLiveSensor()
        {
            LiveRequested = true;
            StartInitialize(null, tryLive: true);
        }

        public void InitializeSensor(Action onComplete)
        {
            StartInitialize(onComplete, tryLive: LiveRequested);
        }

        private void StartInitialize(Action onComplete, bool tryLive)
        {
            if (initializeRoutine != null)
                StopCoroutine(initializeRoutine);

            initializeRoutine = StartCoroutine(InitializeSensorRoutine(onComplete, tryLive));
        }

        private IEnumerator InitializeSensorRoutine(Action onComplete, bool tryLive)
        {
            ShutdownActiveProvider();
            UseMockSensor = true;
            isInitialized = false;

            bool useMockFirst = forceMockSensor || SessionTiming.LaptopPlaytest;
            if (useMockFirst)
            {
                ActivateMockProvider(Application.isEditor
                    ? "Editor testing: mock vitals active. Inject BPM from the Polar panel or keys (+ - P C)."
                    : "Mock sensor forced in Inspector.");
                isInitialized = true;
                onComplete?.Invoke();
                initializeRoutine = null;
                yield break;
            }

            if (!preferRealSensor)
            {
                ActivateMockProvider("Real sensor disabled in Inspector.");
                isInitialized = true;
                onComplete?.Invoke();
                initializeRoutine = null;
                yield break;
            }

            if (enableUdpBridge)
            {
                udpProvider = new UdpHeartRateProvider(udpListenPort);
                bool udpSuccess = false;
                yield return udpProvider.Initialize(success => udpSuccess = success);

                if (udpSuccess)
                {
                    activeProvider = udpProvider;
                    UseMockSensor = false;
                    Debug.Log("SensorManager: Listening for Polar UDP on port " + udpListenPort +
                              ". Run python tools/polar_hr_bridge.py and wear the H10.");
                    isInitialized = true;
                    onComplete?.Invoke();
                    initializeRoutine = null;
                    yield break;
                }

                udpProvider.Shutdown();
                udpProvider = null;
            }

            ActivateMockProvider("No Polar sensor detected. Using simulated vitals.");
            isInitialized = true;
            onComplete?.Invoke();
            initializeRoutine = null;
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
            if (!isInitialized)
                yield return InitializeSensorRoutine(null, tryLive: false);

            float timer = Mathf.Max(1f, durationSeconds);
            var samples = new List<int>();

            while (timer > 0f)
            {
                if (UseMockSensor && mockProvider != null)
                    mockProvider.TickOnce();

                int bpm = CurrentHeartRate;
                if (bpm > 0)
                    samples.Add(bpm);

                yield return new WaitForSeconds(1f);
                timer -= 1f;
            }

            int result;
            if (samples.Count > 0)
                result = Mathf.RoundToInt((float)samples.Average());
            else
                result = CurrentHeartRate;

            if (result <= 0)
                result = 72;

            if (mockProvider != null)
                mockProvider.SetBaseline(result);

            onComplete?.Invoke(result);
            baselineRoutine = null;
        }

        public void StartSessionMonitoring()
        {
            HoldBaselineHeartRate();
        }

        public void HoldBaselineHeartRate()
        {
            if (exposureRoutine != null)
            {
                StopCoroutine(exposureRoutine);
                exposureRoutine = null;
            }

            if (UseMockSensor && mockProvider != null)
                mockProvider.HoldBaseline();
        }

        public void StartExposureMonitoring()
        {
            if (exposureRoutine != null)
            {
                StopCoroutine(exposureRoutine);
                exposureRoutine = null;
            }

            if (UseMockSensor && mockProvider != null)
            {
                Debug.Log("SensorManager: Starting mock exposure monitoring.");
                exposureRoutine = StartCoroutine(mockProvider.RunExposureLoop());
            }
            else
            {
                Debug.Log("SensorManager: Using live Polar heart rate during exposure.");
            }
        }

        public void StopSessionMonitoring()
        {
            if (exposureRoutine != null)
            {
                StopCoroutine(exposureRoutine);
                exposureRoutine = null;
            }

            if (UseMockSensor && mockProvider != null)
                mockProvider.Shutdown();

            Debug.Log("SensorManager: Stopping sensor session monitoring.");
        }

        public void InjectHeartRateOffset(int delta)
        {
            if (UseMockSensor && mockProvider != null)
                mockProvider.InjectOffset(delta);
        }

        public void SetMockHeartRate(int bpm)
        {
            if (UseMockSensor && mockProvider != null)
                mockProvider.SetAbsoluteBpm(bpm);
        }

        public static void EnsureInstanceExists()
        {
            if (Instance == null)
            {
                GameObject go = new GameObject("SensorManager");
                Instance = go.AddComponent<SensorManager>();
            }

            if (Application.isEditor && Instance != null && !Instance.isInitialized && Instance.initializeRoutine == null)
                Instance.InitializeSensor();
        }

        public static string GetBaselineInstruction()
        {
            EnsureInstanceExists();
            if (Instance == null)
                return "Initializing heart rate sensor...";

            if (Instance.IsSensorConnected)
                return "Polar sensor connected. Relax, breathe normally, and stay still.";

            if (!Instance.UseMockSensor)
                return "Listening for Polar on UDP 7878. Wear the H10 and keep python tools/polar_hr_bridge.py running.";

            if (Application.isEditor || Instance.forceMockSensor || Instance.UseMockSensor)
                return "Testing without Polar: simulated vitals are running. Use the Polar panel or keys + / - / P / C to change BPM. F1 skips timers.";

            return "Wear the Polar H10, then continue.";
        }

        private void ActivateMockProvider(string reason)
        {
            ShutdownActiveProvider();
            mockProvider = new MockHeartRateProvider();
            activeProvider = mockProvider;
            UseMockSensor = true;
            StartCoroutine(mockProvider.Initialize(_ => { }));
            if (Application.isEditor)
                Debug.Log($"SensorManager: {reason}");
            else
                Debug.LogWarning($"SensorManager: {reason}");
        }

        private void ShutdownActiveProvider()
        {
            activeProvider?.Shutdown();
            activeProvider = null;
            mockProvider = null;
            udpProvider = null;
        }

        private void OnDestroy()
        {
            ShutdownActiveProvider();
        }
    }
}
