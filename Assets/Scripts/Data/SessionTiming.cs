using UnityEngine;

namespace PhobiaReliefTherapy.Data
{
    /// <summary>
    /// SRS live demo: 60s baseline / 120s exposure, Polar H10 required.
    /// </summary>
    public static class SessionTiming
    {
        /// <summary>
        /// False = live Polar UDP + SRS timers. True = mock vitals and short laptop timers.
        /// </summary>
        public static bool LaptopPlaytest => false;

        public static bool SkipRequested { get; set; }

        public static float BaselineSeconds => 60f;
        public static float ExposureSeconds => 120f;
        public static float SafeRoomCountdownSeconds => 5f;

        public static bool ConsumeSkip()
        {
            if (!SkipRequested)
                return false;

            SkipRequested = false;
            return true;
        }
    }
}
