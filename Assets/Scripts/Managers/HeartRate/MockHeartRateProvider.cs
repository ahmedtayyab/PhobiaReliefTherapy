using System;
using System.Collections;
using UnityEngine;

namespace PhobiaReliefTherapy.Managers.HeartRate
{
    /// <summary>
    /// Simulated Polar H10: heart rate holds at a setpoint (like a real person)
    /// and only drifts a few BPM per second toward that setpoint.
    /// </summary>
    public class MockHeartRateProvider : IHeartRateProvider
    {
        public string SourceName => "Mock Sensor";
        public bool IsConnected => true;
        public int LatestHeartRate { get; private set; }
        public string StatusMessage { get; private set; } = "Simulated heart rate for editor testing.";

        private int restBpm = 72;
        private float setpointBpm = 72f;
        private float currentBpm = 72f;
        private bool exposureMode;

        public IEnumerator Initialize(Action<bool> onComplete)
        {
            restBpm = RandomResting();
            setpointBpm = restBpm;
            currentBpm = restBpm;
            LatestHeartRate = restBpm;
            StatusMessage = "Mock sensor ready (physiological range 40–180 BPM).";
            onComplete?.Invoke(true);
            yield break;
        }

        public void Shutdown()
        {
            exposureMode = false;
        }

        public void SetBaseline(int baseline)
        {
            if (baseline <= 0)
                return;

            restBpm = HeartRateBleConstants.ClampTherapyBpm(baseline);
        }

        public void HoldBaseline()
        {
            exposureMode = false;
            Publish();
        }

        public void InjectOffset(int bpmDelta)
        {
            setpointBpm = HeartRateBleConstants.ClampTherapyBpm(Mathf.RoundToInt(setpointBpm) + bpmDelta);
            currentBpm = Mathf.MoveTowards(currentBpm, setpointBpm, HeartRateBleConstants.InjectStepBpm);
            Publish();
        }

        public void SetAbsoluteBpm(int bpm)
        {
            setpointBpm = HeartRateBleConstants.ClampTherapyBpm(bpm);
            currentBpm = setpointBpm;
            Publish();
        }

        public void TickOnce()
        {
            TickPhysiology();
        }

        public IEnumerator RunBaseline(float durationSeconds)
        {
            float timer = Mathf.Max(1f, durationSeconds);

            while (timer > 0f)
            {
                TickPhysiology();
                yield return new WaitForSeconds(1f);
                timer -= 1f;
            }
        }

        public IEnumerator RunExposureLoop()
        {
            exposureMode = true;
            while (exposureMode)
            {
                TickPhysiology();
                yield return new WaitForSeconds(1f);
            }
        }

        public int GetBaselineResult()
        {
            return HeartRateBleConstants.ClampTherapyBpm(Mathf.RoundToInt(currentBpm));
        }

        private void TickPhysiology()
        {
            float sinus = UnityEngine.Random.Range(-1.5f, 1.5f);
            currentBpm = Mathf.MoveTowards(currentBpm, setpointBpm + sinus, 3f);
            Publish();
        }

        private void Publish()
        {
            LatestHeartRate = HeartRateBleConstants.ClampTherapyBpm(Mathf.RoundToInt(currentBpm));
        }

        private static int RandomResting()
        {
            return UnityEngine.Random.Range(
                HeartRateBleConstants.RestingMinBpm,
                HeartRateBleConstants.RestingMaxBpm + 1);
        }
    }
}
