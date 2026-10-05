namespace PhobiaReliefTherapy.Managers.HeartRate
{
    public static class HeartRateBleConstants
    {
        public const string HeartRateServiceUuid = "0000180d-0000-1000-8000-00805f9b34fb";
        public const string HeartRateMeasurementUuid = "00002a37-0000-1000-8000-00805f9b34fb";
        public const string DefaultPolarNameFilter = "Polar";
        public const int DefaultUdpPort = 7878;

        /// <summary>Polar H10 measurement range (device spec).</summary>
        public const int DeviceMinBpm = 30;
        public const int DeviceMaxBpm = 220;

        /// <summary>Plausible resting-to-panic range during seated VR exposure.</summary>
        public const int TherapyMinBpm = 40;
        public const int TherapyMaxBpm = 180;
        public const int RestingMinBpm = 55;
        public const int RestingMaxBpm = 95;
        public const int StageAdvancedMaxBpm = 95;
        public const int StageIntermediateMaxBpm = 108;
        public const int InjectStepBpm = 8;
        public const int MildAnxietyDeltaBpm = 18;
        public const int PanicDeltaBpm = 32;

        public static int ClampTherapyBpm(int bpm)
        {
            if (bpm <= 0)
                return 0;
            return UnityEngine.Mathf.Clamp(bpm, TherapyMinBpm, TherapyMaxBpm);
        }

        public static int ClampDeviceBpm(int bpm)
        {
            if (bpm <= 0)
                return 0;
            return UnityEngine.Mathf.Clamp(bpm, DeviceMinBpm, DeviceMaxBpm);
        }
    }
}
