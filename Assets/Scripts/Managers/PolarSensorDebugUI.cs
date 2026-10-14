using UnityEngine;
using UnityEngine.SceneManagement;
using PhobiaReliefTherapy.Managers;

namespace PhobiaReliefTherapy.Managers
{
    /// <summary>
    /// Editor-only Polar/mock controls. Hidden on menu screens so it does not block buttons.
    /// </summary>
    public class PolarSensorDebugUI : MonoBehaviour
    {
        private SensorManager sensorManager;
        private Rect windowRect = new Rect(12f, 12f, 280f, 220f);
        private bool collapsed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (!Application.isEditor)
                return;
            if (Object.FindObjectOfType<PolarSensorDebugUI>() != null)
                return;

            var go = new GameObject("PolarSensorDebugUI");
            go.AddComponent<PolarSensorDebugUI>();
            Object.DontDestroyOnLoad(go);
        }

        private void Awake()
        {
            if (!Application.isEditor)
            {
                Destroy(gameObject);
                return;
            }

            SensorManager.EnsureInstanceExists();
            sensorManager = SensorManager.Instance;
        }

        private void Update()
        {
            if (!Application.isEditor)
                return;

            if (sensorManager == null)
                sensorManager = SensorManager.Instance;

            if (Input.GetKeyDown(KeyCode.F8))
                collapsed = !collapsed;
        }

        private static bool ShouldShow()
        {
            string scene = SceneManager.GetActiveScene().name;
            return scene == "BaselineScene"
                || scene == "SafeRoomScene"
                || scene == "HeightScene"
                || scene == "DarknessScene"
                || scene == "CrowdScene"
                || scene == "FeedbackScene";
        }

        private void OnGUI()
        {
            if (!Application.isEditor || !ShouldShow() || sensorManager == null)
                return;

            windowRect.height = collapsed ? 72f : 320f;
            windowRect = GUI.Window(987654, windowRect, DrawWindow, "Editor vitals");
        }

        private void DrawWindow(int id)
        {
            bool mock = sensorManager.UseMockSensor;
            GUILayout.Label(mock
                ? $"Mock - {sensorManager.CurrentHeartRate} BPM  (40-180)"
                : sensorManager.IsSensorConnected
                    ? $"Live Polar - {sensorManager.CurrentHeartRate} BPM"
                    : $"Waiting for Polar - {sensorManager.CurrentHeartRate} BPM");
            GUILayout.Label(sensorManager.SensorStatusMessage);

            if (collapsed)
            {
                if (GUILayout.Button("Expand (F8)"))
                    collapsed = false;
                GUI.DragWindow(new Rect(0f, 0f, 10000f, 20f));
                return;
            }

            if (mock)
            {
                GUILayout.Label("Mock inject (forceMockSensor only). Panic uses +32 BPM and a head jerk.");
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("HR +8"))
                    sensorManager.InjectHeartRateOffset(HeartRate.HeartRateBleConstants.InjectStepBpm);
                if (GUILayout.Button("HR -8"))
                    sensorManager.InjectHeartRateOffset(-HeartRate.HeartRateBleConstants.InjectStepBpm);
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Mild anxiety"))
                {
                    int baseline = BaselineOrDefault();
                    sensorManager.SetMockHeartRate(baseline + HeartRate.HeartRateBleConstants.MildAnxietyDeltaBpm);
                }
                if (GUILayout.Button("Simulate Panic"))
                {
                    int baseline = BaselineOrDefault();
                    sensorManager.SetMockHeartRate(baseline + HeartRate.HeartRateBleConstants.PanicDeltaBpm);
                    HeadMovementTracker.EnsureInstanceExists();
                    HeadMovementTracker.Instance.InjectJerkBurst();
                }
                GUILayout.EndHorizontal();
                if (GUILayout.Button("Simulate Calm"))
                    sensorManager.SetMockHeartRate(BaselineOrDefault());
                if (GUILayout.Button("Inject Head Jerk"))
                {
                    HeadMovementTracker.EnsureInstanceExists();
                    HeadMovementTracker.Instance.InjectJerkBurst();
                }
                GUILayout.Space(4f);
            }

            if (GUILayout.Button(mock ? "Try live Polar (UDP)" : "Reconnect Polar (UDP)"))
                sensorManager.ReconnectLiveSensor();
            if (GUILayout.Button("Collapse (F8)"))
                collapsed = true;

            GUI.DragWindow(new Rect(0f, 0f, 10000f, 20f));
        }

        private static int BaselineOrDefault()
        {
            return PhobiaReliefTherapy.Data.UserData.BaselineHeartRate > 0
                ? PhobiaReliefTherapy.Data.UserData.BaselineHeartRate
                : 72;
        }
    }
}
