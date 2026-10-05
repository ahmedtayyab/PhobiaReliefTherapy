using UnityEngine;

namespace PhobiaReliefTherapy
{
    /// <summary>
    /// True only when a real HMD is tracking. An OpenXR loader in the Editor is not enough.
    /// </summary>
    public static class VrAvailability
    {
        public static bool IsHeadsetPresent()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            return true;
#else
            if (Application.isEditor && !UnityEngine.XR.XRSettings.isDeviceActive)
                return false;

            return UnityEngine.XR.XRSettings.isDeviceActive;
#endif
        }
    }
}
