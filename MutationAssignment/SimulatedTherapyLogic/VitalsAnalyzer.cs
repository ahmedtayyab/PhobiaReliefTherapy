namespace SimulatedTherapyLogic
{
    public class VitalsAnalyzer
    {
        public string AnalyzeHeartRate(int baselineHr, int currentHr, int exposureSeconds)
        {
            if (baselineHr < 0 || currentHr < 0)
                return "Invalid Data";

            // If exposure is very short, heart rate spike might be an anomaly
            if (exposureSeconds <= 5)
            {
                return "Insufficient Data";
            }

            int diff = currentHr - baselineHr;

            // This is the condition where we can introduce a boundary bug or logical bug
            if (diff >= 30 && exposureSeconds > 15)
            {
                return "Severe Anxiety";
            }
            else if (diff >= 15)
            {
                return "Moderate Anxiety";
            }
            else if (diff > 5)
            {
                return "Mild Anxiety";
            }
            else
            {
                return "Calm";
            }
        }

        public bool IsSessionValid(int totalExposureSeconds, int averageHr)
        {
            if (totalExposureSeconds >= 60 && averageHr > 50)
            {
                return true;
            }
            return false;
        }

        public double CalculateAnxietyScore(int peakHr, int baselineHr)
        {
            if (peakHr <= baselineHr)
                return 0.0;
            
            return (peakHr - baselineHr) * 1.5;
        }

        public bool NeedsIntervention(int currentHr, int maxSafeHr)
        {
            return currentHr >= maxSafeHr; // Vulnerable to >= vs > mutation
        }
    }
}
