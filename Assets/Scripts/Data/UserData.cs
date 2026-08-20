namespace PhobiaReliefTherapy.Data
{
    /// <summary>
    /// Static class to persist user data across scenes during the current session.
    /// </summary>
    public static class UserData
    {
        public static string UserId { get; set; } = "";
        public static string Username { get; set; } = "Guest";
        
        // E.g. "Height", "Darkness", "Crowd"
        public static string SelectedPhobia { get; set; } = "None";
        
        // E.g. "Low", "Medium", "High"
        public static string SelectedDifficulty { get; set; } = "Low";
        
        // Simulated or real baseline heart rate before starting exposure
        public static int BaselineHeartRate { get; set; } = 0;

        // For session tracking
        public static int CurrentStage { get; set; } = 1;

        public static bool IsAdmin { get; set; } = false;
        public static bool SessionWasAborted { get; set; } = false;

        public static void ResetSessionState()
        {
            SessionWasAborted = false;
            SessionMetrics.Reset();
        }
    }
}
