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
        public static event System.Action<PanicDetectionService.PanicEvaluation> EvaluationReady;

        private Coroutine monitorRoutine;
        private float consecutiveStressSeconds;
        private float elapsedSeconds;
        private float monitoringStartedAt;
        private bool safetyTriggered;
        private System.Action<PanicDetectionService.PanicEvaluation> onSafetyTriggered;

        private const float SafetyGraceSeconds = 20f;

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
            safetyTriggered = false;
            monitoringStartedAt = Time.time;

            float savedBaselineHeadMovement = SessionMetrics.BaselineHeadMovementScore;
            SessionMetrics.Reset();
            SessionMetrics.BaselineHeadMovementScore = savedBaselineHeadMovement;
            SessionMetrics.SessionActive = true;
            SessionMetrics.SessionStartTime = Time.time;
            elapsedSeconds = 0f;
            consecutiveStressSeconds = 0f;

            SensorManager.EnsureInstanceExists();
            HeadMovementTracker.EnsureInstanceExists();
            AdaptiveExposureController.EnsureInstanceExists();
            AdaptiveExposureController.Instance.BeginSessionOverlay();
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
            if (SensorManager.Instance != null)
                SensorManager.Instance.StopSessionMonitoring();
            if (AdaptiveExposureController.Instance != null)
                AdaptiveExposureController.Instance.EndSessionOverlay();
        }

        public void CaptureBaselineHeadMovement()
        {
            HeadMovementTracker.EnsureInstanceExists();
            if (SessionMetrics.HeadMovementSampleCount > 0)
            {
                SessionMetrics.BaselineHeadMovementScore = SessionMetrics.AverageHeadMovementScore;
                return;
            }

            if (HeadMovementTracker.Instance != null)
                SessionMetrics.BaselineHeadMovementScore = HeadMovementTracker.Instance.AgitationScore;
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
                movementScore = HeadMovementTracker.Instance.AgitationScore;
                SessionMetrics.RecordHeadMovementSample(movementScore);
            }

            float deltaHr = heartRate - UserData.BaselineHeartRate;
            float movementFloor = Mathf.Max(15f, SessionMetrics.BaselineHeadMovementScore);
            if (deltaHr > 16f || movementScore > movementFloor * 2.4f)
                consecutiveStressSeconds += 1f;
            else
                consecutiveStressSeconds = 0f;

            var evaluation = PanicDetectionService.Evaluate(heartRate, movementScore, consecutiveStressSeconds);
            EvaluationReady?.Invoke(evaluation);

            if (evaluation.PanicScore >= 10f)
            {
                SessionMetrics.RecordPanicEvent(PanicDetectionService.ToRecord(evaluation));
            }

            if (evaluation.TriggerSafetyMechanism &&
                !safetyTriggered &&
                Time.time - monitoringStartedAt >= SafetyGraceSeconds)
            {
                safetyTriggered = true;
                SessionMetrics.SafetyTriggerCount++;
                SessionMetrics.SessionActive = false;
                if (AdaptiveExposureController.Instance != null)
                    AdaptiveExposureController.Instance.HandleSeverePanic(evaluation);
                else
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
