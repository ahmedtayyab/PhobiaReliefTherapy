using System;
using PhobiaReliefTherapy.Data;

namespace PhobiaReliefTherapy.Therapy
{
    /// <summary>
    /// SRS §3.2.5 / UC-10 panic score and AI-style recommendations.
    /// Uses the documented weighted formula and classification thresholds.
    /// </summary>
    public static class PanicDetectionService
    {
        public const float DeltaHrHighPriorityThreshold = 32f;
        public const float DeltaHrImmediateEvaluateThreshold = 36f;
        public const float SafeRoomScoreThreshold = 22f;

        public struct PanicEvaluation
        {
            public float DeltaHeartRate;
            public float HeadMovementScore;
            public float TimeSpentScore;
            public float PanicScore;
            public string Classification;
            public string AiRecommendation;
            public bool TriggerSafetyMechanism;
        }

        public static PanicEvaluation Evaluate(int currentHeartRate, float headMovementScore, float consecutiveStressSeconds)
        {
            float baselineHr = Math.Max(1, UserData.BaselineHeartRate);
            float deltaHr = currentHeartRate - baselineHr;

            float hmScore = ComputeHeadMovementScore(headMovementScore);
            float tsScore = ComputeTimeSpentScore(consecutiveStressSeconds, deltaHr);

            float panicScore = (deltaHr * 0.5f) + (hmScore * 0.3f) + (tsScore * 0.2f);
            if (panicScore < 0f)
                panicScore = 0f;

            if (deltaHr > DeltaHrImmediateEvaluateThreshold)
                panicScore = Math.Max(panicScore, SafeRoomScoreThreshold);

            if (deltaHr > DeltaHrHighPriorityThreshold && panicScore < 10f)
                panicScore = Math.Max(panicScore, 10f);

            var evaluation = new PanicEvaluation
            {
                DeltaHeartRate = deltaHr,
                HeadMovementScore = hmScore,
                TimeSpentScore = tsScore,
                PanicScore = panicScore
            };

            if (panicScore < 10f)
            {
                evaluation.Classification = "Calm Stress";
                evaluation.AiRecommendation = "Continue Exposure";
            }
            else if (panicScore < 18f)
            {
                evaluation.Classification = "Panic Detected";
                evaluation.AiRecommendation = "Repeat Level";
            }
            else
            {
                evaluation.Classification = "Severe Panic";
                evaluation.AiRecommendation = "Stop / Safe Room";
                evaluation.TriggerSafetyMechanism = panicScore >= SafeRoomScoreThreshold && deltaHr >= 20f;
            }

            return evaluation;
        }

        private static float ComputeHeadMovementScore(float currentMovement)
        {
            float baseline = Math.Max(15f, SessionMetrics.BaselineHeadMovementScore);
            float excess = currentMovement - baseline;
            if (excess <= 0f)
                return 0f;

            return Math.Min(100f, excess / baseline * 45f);
        }

        private static float ComputeTimeSpentScore(float consecutiveStressSeconds, float deltaHr)
        {
            if (consecutiveStressSeconds <= 0f || deltaHr <= 5f)
                return 0f;

            if (consecutiveStressSeconds < 15f)
                return Math.Min(100f, consecutiveStressSeconds / 15f * 100f);

            return 100f;
        }

        public static PanicEventRecord ToRecord(PanicEvaluation evaluation)
        {
            return new PanicEventRecord
            {
                Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                DeltaHeartRate = evaluation.DeltaHeartRate,
                HeadMovementScore = evaluation.HeadMovementScore,
                TimeSpentScore = evaluation.TimeSpentScore,
                PanicScore = evaluation.PanicScore,
                Classification = evaluation.Classification,
                AiRecommendation = evaluation.AiRecommendation
            };
        }
    }
}
