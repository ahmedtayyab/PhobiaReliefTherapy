using System.Collections;
using UnityEngine;
using PhobiaReliefTherapy.Data;
using PhobiaReliefTherapy.Managers;

namespace PhobiaReliefTherapy.Therapy
{
    /// <summary>
    /// Samples HR and head movement every second during baseline/exposure (SRS UC-08).
    /// </summary>
    public class ExposureSessionMonitor : MonoBehaviour
    {
        public static ExposureSessionMonitor Instance { get; private set; }

        private Coroutine monitorRoutine;
        private float consecutiveStressSeconds;
        private float elapsedSeconds;
        private System.Action<PanicDetectionService.PanicEvaluation> onSafetyTriggered;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
            }
        }

        public void StartMonitoring(System.Action<PanicDetectionService.PanicEvaluation> safetyCallback = null)
        {
            StopMonitoring(false);
            onSafetyTriggered = safetyCallback;
            SessionMetrics.Reset();
            SessionMetrics.SessionActive = true;
            SessionMetrics.SessionStartTime = Time.time;
            elapsedSeconds = 0f;
            consecutiveStressSeconds = 0f;

            SensorManager.EnsureInstanceExists();
            HeadMovementTracker.EnsureInstanceExists();
            SensorManager.Instance.StartExposureMonitoring();

            monitorRoutine = StartCoroutine(MonitorLoop());
        }

        public void StopMonitoring(bool aborted)
        {
            if (monitorRoutine != null)
            {
                StopCoroutine(monitorRoutine);
                monitorRoutine = null;
            }

            SessionMetrics.SessionAborted = aborted;
            SessionMetrics.FinalizeDuration(elapsedSeconds);
            SensorManager.Instance.StopSessionMonitoring();
        }

        public void CaptureBaselineHeadMovement()
        {
            HeadMovementTracker.EnsureInstanceExists();
            if (HeadMovementTracker.Instance != null)
            {
                SessionMetrics.BaselineHeadMovementScore = HeadMovementTracker.Instance.CombinedMovementScore;
            }
        }

        private IEnumerator MonitorLoop()
        {
            while (SessionMetrics.SessionActive)
            {
                SampleOnce();
                elapsedSeconds += 1f;
                yield return new WaitForSeconds(1f);
            }
        }

        private void SampleOnce()
        {
            SensorManager.EnsureInstanceExists();
            int heartRate = SensorManager.Instance.CurrentHeartRate;
            SessionMetrics.RecordHeartRateSample(heartRate);

            float movementScore = 0f;
            if (HeadMovementTracker.Instance != null)
            {
                movementScore = HeadMovementTracker.Instance.CombinedMovementScore;
                SessionMetrics.RecordHeadMovementSample(movementScore);
            }

            float deltaHr = heartRate - UserData.BaselineHeartRate;
            if (deltaHr > 10f || movementScore > SessionMetrics.BaselineHeadMovementScore * 1.5f)
                consecutiveStressSeconds += 1f;
            else
                consecutiveStressSeconds = 0f;

            var evaluation = PanicDetectionService.Evaluate(heartRate, movementScore, consecutiveStressSeconds);

            if (evaluation.PanicScore >= 10f)
            {
                SessionMetrics.RecordPanicEvent(PanicDetectionService.ToRecord(evaluation));
            }

            if (evaluation.TriggerSafetyMechanism)
            {
                SessionMetrics.SafetyTriggerCount++;
                onSafetyTriggered?.Invoke(evaluation);
            }
        }

        public static void EnsureInstanceExists()
        {
            if (Instance == null)
            {
                var go = new GameObject("ExposureSessionMonitor");
                go.AddComponent<ExposureSessionMonitor>();
            }
        }
    }
}
