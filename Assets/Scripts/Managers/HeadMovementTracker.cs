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
        public float AgitationScore { get; private set; }
        public float JerkScore { get; private set; }
        public float FreezeScore { get; private set; }

        private Quaternion lastRotation;
        private Vector3 lastPosition;
        private float lastRotationSpeed;
        private float positionChangeAccumulator;
        private float sampleWindow = 1f;
        private float windowTimer;
        private bool initialized;
        private float injectedJerk;
        private float injectedFreeze;

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

            if (!initialized)
            {
                lastRotation = target.rotation;
                lastPosition = target.position;
                initialized = true;
                return;
            }

            float deltaTime = Mathf.Max(Time.deltaTime, 0.0001f);
            float angleDelta = Quaternion.Angle(lastRotation, target.rotation);
            CurrentRotationSpeed = angleDelta / deltaTime;

            float rotationAccel = Mathf.Abs(CurrentRotationSpeed - lastRotationSpeed) / deltaTime;
            JerkScore = Mathf.Max(injectedJerk, Mathf.Min(100f, rotationAccel / 40f));
            injectedJerk = Mathf.MoveTowards(injectedJerk, 0f, deltaTime * 40f);

            float positionDelta = Vector3.Distance(lastPosition, target.position);
            positionChangeAccumulator += positionDelta;

            windowTimer += deltaTime;
            if (windowTimer >= sampleWindow)
            {
                CurrentMovementFrequency = positionChangeAccumulator / sampleWindow;
                positionChangeAccumulator = 0f;
                windowTimer = 0f;
            }

            bool freezing = CurrentRotationSpeed < 2f && CurrentMovementFrequency < 0.02f;
            FreezeScore = Mathf.Max(injectedFreeze, freezing ? Mathf.Min(100f, FreezeScore + deltaTime * 25f) : Mathf.MoveTowards(FreezeScore, 0f, deltaTime * 40f));
            injectedFreeze = Mathf.MoveTowards(injectedFreeze, 0f, deltaTime * 30f);

            CombinedMovementScore =
                (CurrentRotationSpeed * 0.45f) +
                (CurrentMovementFrequency * 24f) +
                (JerkScore * 0.35f) +
                (FreezeScore * 0.25f);

            AgitationScore =
                (CurrentRotationSpeed * 0.45f) +
                (CurrentMovementFrequency * 24f) +
                (JerkScore * 0.35f);

            lastRotationSpeed = CurrentRotationSpeed;
            lastRotation = target.rotation;
            lastPosition = target.position;
        }

        public void InjectJerkBurst()
        {
            injectedJerk = 65f;
        }

        public void InjectFreeze()
        {
            injectedFreeze = 70f;
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
