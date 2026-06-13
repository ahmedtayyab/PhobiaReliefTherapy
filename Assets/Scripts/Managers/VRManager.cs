using UnityEngine;
using UnityEngine.XR;
using UnityEngine.SceneManagement;

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

    /// <summary>
    /// Basic headset locomotion for testing: left stick moves the scene root, right stick snap-turns it.
    /// If the camera has a parent, that transform is moved; otherwise the camera transform itself is used.
    /// </summary>
    public class VRLocomotionBridge : MonoBehaviour
    {
        private static VRLocomotionBridge instance;

        [Header("Movement")]
        [SerializeField] private float moveSpeed = 1.8f;
        [SerializeField] private float turnSpeed = 60f;
        [SerializeField] private float snapTurnAngle = 30f;
        [SerializeField] private float deadZone = 0.2f;
        [SerializeField] private float snapThreshold = 0.75f;

        private Transform rigTransform;
        private bool hasSnappedLeft;
        private bool hasSnappedRight;

        public static void EnsureInstanceExists()
        {
            if (instance != null)
                return;

            GameObject existing = Object.FindObjectOfType<VRLocomotionBridge>()?.gameObject;
            if (existing != null)
            {
                instance = existing.GetComponent<VRLocomotionBridge>();
                return;
            }

            GameObject go = new GameObject("VRLocomotionBridge");
            instance = go.AddComponent<VRLocomotionBridge>();
            Object.DontDestroyOnLoad(go);
        }

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                Object.DontDestroyOnLoad(gameObject);
            }
            else if (instance != this)
            {
                Destroy(gameObject);
                return;
            }

            CacheRigTransform();
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                SceneManager.sceneLoaded -= OnSceneLoaded;
                instance = null;
            }
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            CacheRigTransform();
            hasSnappedLeft = false;
            hasSnappedRight = false;
        }

        private void CacheRigTransform()
        {
            Camera camera = Camera.main;
            if (camera != null && camera.transform.parent != null)
                rigTransform = camera.transform.parent;
            else if (camera != null)
                rigTransform = camera.transform;
            else
                rigTransform = transform;
        }

        private void Update()
        {
            if (rigTransform == null)
                CacheRigTransform();

            Vector2 moveInput = GetMoveInput();
            if (moveInput.sqrMagnitude > deadZone * deadZone)
            {
                Vector3 forward = GetFlatForward();
                Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
                Vector3 movement = (forward * moveInput.y + right * moveInput.x) * (moveSpeed * Time.deltaTime);
                rigTransform.position += movement;
            }

            float turnInput = GetTurnInput();
            if (turnInput <= -snapThreshold)
            {
                if (!hasSnappedLeft)
                {
                    rigTransform.Rotate(0f, -snapTurnAngle, 0f, Space.World);
                    hasSnappedLeft = true;
                }
            }
            else
            {
                hasSnappedLeft = false;
            }

            if (turnInput >= snapThreshold)
            {
                if (!hasSnappedRight)
                {
                    rigTransform.Rotate(0f, snapTurnAngle, 0f, Space.World);
                    hasSnappedRight = true;
                }
            }
            else
            {
                hasSnappedRight = false;
            }

            if (Application.isEditor)
            {
                ApplyEditorFallback();
            }
        }

        private Vector2 GetMoveInput()
        {
            if (Application.isEditor)
                return new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));

            return GetXRStick(XRNode.LeftHand);
        }

        private float GetTurnInput()
        {
            if (Application.isEditor)
                return Input.GetAxisRaw("Mouse X");

            return GetXRStick(XRNode.RightHand).x;
        }

        private Vector2 GetXRStick(XRNode node)
        {
            InputDevice device = InputDevices.GetDeviceAtXRNode(node);
            if (!device.isValid)
                return Vector2.zero;

            if (device.TryGetFeatureValue(CommonUsages.primary2DAxis, out Vector2 axis))
                return axis;

            return Vector2.zero;
        }

        private Vector3 GetFlatForward()
        {
            Transform reference = Camera.main != null ? Camera.main.transform : rigTransform;
            Vector3 forward = Vector3.ProjectOnPlane(reference.forward, Vector3.up);
            if (forward.sqrMagnitude < 0.001f)
                forward = Vector3.forward;

            return forward.normalized;
        }

        private void ApplyEditorFallback()
        {
            float yaw = 0f;
            if (Input.GetKey(KeyCode.Q))
                yaw = -turnSpeed * Time.deltaTime;
            else if (Input.GetKey(KeyCode.E))
                yaw = turnSpeed * Time.deltaTime;

            if (Mathf.Abs(yaw) > 0f)
            {
                rigTransform.Rotate(0f, yaw, 0f, Space.World);
            }
        }
    }
}
