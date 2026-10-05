using UnityEngine;
using PhobiaReliefTherapy.Data;
using PhobiaReliefTherapy.Managers;
using PhobiaReliefTherapy.Managers.HeartRate;

namespace PhobiaReliefTherapy.Testing
{
    /// <summary>
    /// Keyboard helpers so the full therapy loop can be exercised in the Editor without VR.
    /// F1 skip timer · Equals raise HR · Minus lower HR · J jerk · LeftBracket freeze · P panic · C calm
    /// </summary>
    public class EditorTherapySimulator : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (!Application.isEditor)
                return;
            if (Object.FindObjectOfType<EditorTherapySimulator>() != null)
                return;

            var go = new GameObject("EditorTherapySimulator");
            go.AddComponent<EditorTherapySimulator>();
            Object.DontDestroyOnLoad(go);
        }

        private void Start()
        {
            if (!Application.isEditor)
                return;

            Debug.Log(SrsLogicTests.RunAll());
        }

        private void Update()
        {
            if (!Application.isEditor)
                return;

            if (Input.GetKeyDown(KeyCode.F1))
                SessionTiming.SkipRequested = true;

            SensorManager.EnsureInstanceExists();
            HeadMovementTracker.EnsureInstanceExists();

            if (Input.GetKeyDown(KeyCode.Equals) || Input.GetKeyDown(KeyCode.Plus))
                SensorManager.Instance.InjectHeartRateOffset(HeartRateBleConstants.InjectStepBpm);

            if (Input.GetKeyDown(KeyCode.Minus))
                SensorManager.Instance.InjectHeartRateOffset(-HeartRateBleConstants.InjectStepBpm);

            if (Input.GetKeyDown(KeyCode.J))
                HeadMovementTracker.Instance.InjectJerkBurst();

            if (Input.GetKeyDown(KeyCode.LeftBracket))
                HeadMovementTracker.Instance.InjectFreeze();

            if (Input.GetKeyDown(KeyCode.P))
            {
                int baseline = UserData.BaselineHeartRate > 0 ? UserData.BaselineHeartRate : 72;
                SensorManager.Instance.SetMockHeartRate(baseline + HeartRateBleConstants.PanicDeltaBpm);
                HeadMovementTracker.Instance.InjectJerkBurst();
            }

            if (Input.GetKeyDown(KeyCode.C))
            {
                int baseline = UserData.BaselineHeartRate > 0 ? UserData.BaselineHeartRate : 72;
                SensorManager.Instance.SetMockHeartRate(baseline);
            }
        }
    }
}
