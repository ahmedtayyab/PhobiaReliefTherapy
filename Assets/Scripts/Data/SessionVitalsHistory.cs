using System.Collections.Generic;

namespace PhobiaReliefTherapy.Data
{
    /// <summary>
    /// Rolling session series for live graphs and UC-11 review.
    /// Heart rate, head-movement agitation/jerk, and panic score.
    /// </summary>
    public static class SessionVitalsHistory
    {
        public const int MaxSamples = 360;

        public static readonly List<float> HeartRates = new List<float>(MaxSamples);
        public static readonly List<float> MovementScores = new List<float>(MaxSamples);
        public static readonly List<float> JerkScores = new List<float>(MaxSamples);
        public static readonly List<float> PanicScores = new List<float>(MaxSamples);

        public static float LastHeartRate { get; private set; }
        public static float LastMovement { get; private set; }
        public static float LastJerk { get; private set; }
        public static float LastPanic { get; private set; }

        public static void Clear()
        {
            HeartRates.Clear();
            MovementScores.Clear();
            JerkScores.Clear();
            PanicScores.Clear();
            LastHeartRate = 0f;
            LastMovement = 0f;
            LastJerk = 0f;
            LastPanic = 0f;
        }

        public static void Record(float heartRate, float movement, float jerk, float panic)
        {
            LastHeartRate = heartRate;
            LastMovement = movement;
            LastJerk = jerk;
            LastPanic = panic;

            Append(HeartRates, heartRate);
            Append(MovementScores, movement);
            Append(JerkScores, jerk);
            Append(PanicScores, panic);
        }

        private static void Append(List<float> series, float value)
        {
            series.Add(value);
            if (series.Count > MaxSamples)
                series.RemoveAt(0);
        }
    }
}
