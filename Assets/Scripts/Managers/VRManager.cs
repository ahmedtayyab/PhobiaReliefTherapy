using UnityEngine;

namespace PhobiaReliefTherapy.Managers
{
    public enum VRPlatform
    {
        Mock,
        OculusQuest,
        Unknown
    }

    /// <summary>
    /// Abstracts VR availability and runtime state so the app can be tested without hardware.
    /// Replace this class later with real Quest 2/XR plugin initialization.
    /// </summary>
    public class VRManager : MonoBehaviour
    {
        public static VRManager Instance { get; private set; }

        public bool IsVRAvailable { get; private set; }
        public bool IsVRSessionActive { get; private set; }
        public bool UseMockVR { get; private set; } = true;
        public VRPlatform CurrentPlatform { get; private set; } = VRPlatform.Mock;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            DetectVRAvailability();
        }

        private void DetectVRAvailability()
        {
            IsVRAvailable = false;
            UseMockVR = true;
            CurrentPlatform = VRPlatform.Mock;
            Debug.Log("VRManager: VR is currently mocked for development.");
        }

        public void InitializeVR()
        {
            if (IsVRSessionActive)
                return;

            if (IsVRAvailable)
            {
                Debug.Log($"VRManager: Initializing VR session for {CurrentPlatform}.");
                UseMockVR = false;
            }
            else
            {
                Debug.Log("VRManager: Starting mock VR session for editor/mobile testing.");
                UseMockVR = true;
            }

            IsVRSessionActive = true;
        }

        public void StopVRSession()
        {
            if (!IsVRSessionActive)
                return;

            Debug.Log("VRManager: Stopping VR session.");
            IsVRSessionActive = false;
        }

        public Transform GetHeadTransform()
        {
            return Camera.main?.transform;
        }

        public static void EnsureInstanceExists()
        {
            if (Instance == null)
            {
                GameObject go = new GameObject("VRManager");
                Instance = go.AddComponent<VRManager>();
            }
        }
    }
}
