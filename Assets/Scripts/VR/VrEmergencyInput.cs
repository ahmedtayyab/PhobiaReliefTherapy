using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR;
using PhobiaReliefTherapy.Data;
using PhobiaReliefTherapy.Therapy;

namespace PhobiaReliefTherapy.VR
{
    /// <summary>
    /// Quest A (right) / X (left) immediately returns to the Safe Room during exposure.
    /// </summary>
    public class VrEmergencyInput : MonoBehaviour
    {
        private bool primaryWasDown = true;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (Object.FindObjectOfType<VrEmergencyInput>() != null)
                return;

            var go = new GameObject("VrEmergencyInput");
            go.AddComponent<VrEmergencyInput>();
            Object.DontDestroyOnLoad(go);
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            primaryWasDown = true;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            primaryWasDown = true;
        }

        private void Update()
        {
            if (!IsEmergencyScene())
            {
                primaryWasDown = true;
                return;
            }

            bool down = IsPrimaryDown();
            if ((down && !primaryWasDown) || Input.GetKeyDown(KeyCode.X))
            {
                SessionMetrics.SafetyTriggerCount++;
                TherapySessionHelper.AbortToSafeRoom(true);
                primaryWasDown = true;
                return;
            }

            primaryWasDown = down;
        }

        private static bool IsEmergencyScene()
        {
            string scene = SceneManager.GetActiveScene().name;
            return scene == "HeightScene" || scene == "DarknessScene" || scene == "CrowdScene"
                   || SessionMetrics.SessionActive;
        }

        private static bool IsPrimaryDown()
        {
            return IsPrimary(XRNode.RightHand) || IsPrimary(XRNode.LeftHand);
        }

        private static bool IsPrimary(XRNode node)
        {
            InputDevice device = InputDevices.GetDeviceAtXRNode(node);
            return device.isValid &&
                   device.TryGetFeatureValue(CommonUsages.primaryButton, out bool pressed) &&
                   pressed;
        }
    }
}
