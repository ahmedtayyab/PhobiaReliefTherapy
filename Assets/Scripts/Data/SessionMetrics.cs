using System;
using System.Collections.Generic;

namespace PhobiaReliefTherapy.Data
{
    [Serializable]
    public class PanicEventRecord
    {
        public string Timestamp;
        public float DeltaHeartRate;
        public float HeadMovementScore;
        public float TimeSpentScore;
        public float PanicScore;
        public string Classification;
        public string AiRecommendation;
    }

    /// <summary>
    /// Holds per-session physiological and panic metrics (SRS §3.2.4–3.2.8, UC-08/10/11).
    /// </summary>
    public static class SessionMetrics
    {
        public static bool SessionActive { get; set; }
        public static bool SessionAborted { get; set; }
        public static float SessionStartTime { get; set; }
        public static float TotalExposureDuration { get; set; }

        public static int PeakHeartRate { get; set; }
        public static int FinalHeartRate { get; set; }
        public static float AverageHeartRate { get; set; }
        public static int HeartRateSampleCount { get; set; }
        public static int HeartRateSampleSum { get; set; }

        public static float BaselineHeadMovementScore { get; set; }
        public static float PeakHeadMovementScore { get; set; }
        public static float AverageHeadMovementScore { get; set; }
        public static float HeadMovementSampleSum { get; set; }
        public static int HeadMovementSampleCount { get; set; }

        public static int PanicEventCount { get; set; }
        public static int SafetyTriggerCount { get; set; }
        public static float HighestPanicScore { get; set; }
        public static float AveragePanicScore { get; set; }
        public static float PanicScoreSum { get; set; }
        public static string LastAiRecommendation { get; set; } = "";

        public static int UserFeedbackRating { get; set; }
        public static string UserFeedbackComments { get; set; } = "";

        public static List<PanicEventRecord> PanicEvents { get; } = new List<PanicEventRecord>();

        public static void Reset()
        {
            SessionActive = false;
            SessionAborted = false;
            SessionStartTime = 0f;
            TotalExposureDuration = 0f;

            PeakHeartRate = 0;
            FinalHeartRate = 0;
            AverageHeartRate = 0f;
            HeartRateSampleCount = 0;
            HeartRateSampleSum = 0;

            BaselineHeadMovementScore = 0f;
            PeakHeadMovementScore = 0f;
            AverageHeadMovementScore = 0f;
            HeadMovementSampleSum = 0f;
            HeadMovementSampleCount = 0;

            PanicEventCount = 0;
            SafetyTriggerCount = 0;
            HighestPanicScore = 0f;
            AveragePanicScore = 0f;
            PanicScoreSum = 0f;
            LastAiRecommendation = "";

            UserFeedbackRating = 0;
            UserFeedbackComments = "";

            PanicEvents.Clear();
        }

        public static void RecordHeartRateSample(int heartRate)
        {
            if (heartRate <= 0)
                return;

            HeartRateSampleCount++;
            HeartRateSampleSum += heartRate;
            AverageHeartRate = (float)HeartRateSampleSum / HeartRateSampleCount;
            FinalHeartRate = heartRate;

            if (heartRate > PeakHeartRate)
                PeakHeartRate = heartRate;
        }

        public static void RecordHeadMovementSample(float movementScore)
        {
            HeadMovementSampleCount++;
            HeadMovementSampleSum += movementScore;
            AverageHeadMovementScore = HeadMovementSampleSum / HeadMovementSampleCount;

            if (movementScore > PeakHeadMovementScore)
                PeakHeadMovementScore = movementScore;
        }

        public static void RecordPanicEvent(PanicEventRecord record)
        {
            PanicEvents.Add(record);
            PanicEventCount = PanicEvents.Count;
            PanicScoreSum += record.PanicScore;
            AveragePanicScore = PanicScoreSum / PanicEventCount;

            if (record.PanicScore > HighestPanicScore)
                HighestPanicScore = record.PanicScore;

            if (!string.IsNullOrEmpty(record.AiRecommendation))
                LastAiRecommendation = record.AiRecommendation;
        }

        public static void FinalizeDuration(float durationSeconds)
        {
            TotalExposureDuration = durationSeconds;
            SessionActive = false;
        }
    }
}
