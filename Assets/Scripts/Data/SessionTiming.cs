using UnityEngine;

namespace PhobiaReliefTherapy.Data
{
    /// <summary>
        /// Temporary short timings for live VR testing.
        /// Restore SRS: baseline 60s, exposure 120s.
        /// F1 still skips timers in the Editor.
        /// </summary>
    public static class SessionTiming
    {
        public static bool SkipRequested { get; set; }

        public static float BaselineSeconds => 20f;
        public static float ExposureSeconds => 30f;
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
