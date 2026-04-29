namespace PhobiaReliefTherapy.Data
{
    /// <summary>
    /// Static class to persist user data across scenes during the current session.
    /// </summary>
    public static class UserData
    {
        public static string Username { get; set; } = "Guest";
        
        // E.g. "Height", "Darkness", "Crowd"
        public static string SelectedPhobia { get; set; } = "None";
        
        // E.g. "Low", "Medium", "High"
        public static string SelectedDifficulty { get; set; } = "Low";
        
        // Simulated or real baseline heart rate before starting exposure
        public static int BaselineHeartRate { get; set; } = 0;
    }
}
