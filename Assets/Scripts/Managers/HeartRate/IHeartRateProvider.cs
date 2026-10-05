using System;
using System.Collections;

namespace PhobiaReliefTherapy.Managers.HeartRate
{
    public interface IHeartRateProvider
    {
        string SourceName { get; }
        bool IsConnected { get; }
        int LatestHeartRate { get; }
        string StatusMessage { get; }

        IEnumerator Initialize(Action<bool> onComplete);
        void Shutdown();
    }
}
