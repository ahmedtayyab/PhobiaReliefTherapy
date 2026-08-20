using UnityEngine;

namespace PhobiaReliefTherapy.Managers
{
    /// <summary>
    /// Tracks headset rotation speed and movement frequency (SRS §3.2.4, UC-08).
    /// Works in editor (mouse look) and VR (TrackedPoseDriver).
    /// </summary>
    public class HeadMovementTracker : MonoBehaviour
    {
        public static HeadMovementTracker Instance { get; private set; }

        public float CurrentRotationSpeed { get; private set; }
        public float CurrentMovementFrequency { get; private set; }
        public float CombinedMovementScore { get; private set; }

        private Quaternion lastRotation;
        private Vector3 lastPosition;
        private float positionChangeAccumulator;
        private float sampleWindow = 1f;
        private float windowTimer;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
            }
        }

        private void LateUpdate()
        {
            Transform target = Camera.main != null ? Camera.main.transform : null;
            if (target == null)
                return;

            float deltaTime = Mathf.Max(Time.deltaTime, 0.0001f);
            float angleDelta = Quaternion.Angle(lastRotation, target.rotation);
            CurrentRotationSpeed = angleDelta / deltaTime;

            float positionDelta = Vector3.Distance(lastPosition, target.position);
            positionChangeAccumulator += positionDelta;

            windowTimer += deltaTime;
            if (windowTimer >= sampleWindow)
            {
                CurrentMovementFrequency = positionChangeAccumulator / sampleWindow;
                positionChangeAccumulator = 0f;
                windowTimer = 0f;
            }

            CombinedMovementScore = (CurrentRotationSpeed * 0.7f) + (CurrentMovementFrequency * 30f);

            lastRotation = target.rotation;
            lastPosition = target.position;
        }

        public static void EnsureInstanceExists()
        {
            if (Instance == null)
            {
                var go = new GameObject("HeadMovementTracker");
                go.AddComponent<HeadMovementTracker>();
            }
        }
    }
}
