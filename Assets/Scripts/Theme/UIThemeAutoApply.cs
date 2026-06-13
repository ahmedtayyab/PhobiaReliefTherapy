using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using PhobiaReliefTherapy.Managers;
using PhobiaReliefTherapy.VR;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace PhobiaReliefTherapy.Theme
{
    [DefaultExecutionOrder(-100)]
    public class UIThemeAutoApply : MonoBehaviour
    {
        private static UIThemeAutoApply instance;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            if (instance != null)
                return;

            GameObject controller = new GameObject("[UIThemeAutoApply]");
            DontDestroyOnLoad(controller);
            instance = controller.AddComponent<UIThemeAutoApply>();
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void Start()
        {
            ApplyThemeToScene(SceneManager.GetActiveScene());
            StartCoroutine(ConfigureCanvasesRoutine());
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            ApplyThemeToScene(scene);
            StartCoroutine(ConfigureCanvasesRoutine());
        }

        private void ApplyThemeToScene(Scene scene)
        {
            ThemePreset theme = Resources.Load<ThemePreset>("MedicalTheme");
            if (theme == null)
                return;

            ApplyThemeToImages(theme);
            ApplyThemeToTexts(theme);
        }

        private bool IsVRActive()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            return true; // Always force VR mode on Oculus Quest standalone builds
#else
            // Fallback for Editor VR play mode or PC VR builds
            if (UnityEngine.XR.XRSettings.isDeviceActive)
                return true;

            if (UnityEngine.XR.Management.XRGeneralSettings.Instance != null &&
                UnityEngine.XR.Management.XRGeneralSettings.Instance.Manager != null &&
                UnityEngine.XR.Management.XRGeneralSettings.Instance.Manager.activeLoader != null)
            {
                return true;
            }

            return false;
#endif
        }

        private System.Collections.IEnumerator ConfigureCanvasesRoutine()
        {
            if (!IsVRActive())
                yield break;

            float elapsed = 0f;
            while (elapsed < 3.0f)
            {
                Camera mainCamera = Camera.main;
                if (mainCamera == null)
                {
                    mainCamera = FindObjectOfType<Camera>();
                }

                if (mainCamera != null)
                {
                    ConfigureCanvasesForVR(mainCamera);
                    break;
                }

                elapsed += 0.1f;
                yield return new WaitForSeconds(0.1f);
            }
        }

        private float checkInterval = 1.0f;
        private float nextCheckTime = 0f;

        private void Update()
        {
            if (IsVRActive())
            {
                if (Time.time >= nextCheckTime)
                {
                    nextCheckTime = Time.time + checkInterval;
                    Camera mainCamera = Camera.main;
                    if (mainCamera == null)
                    {
                        mainCamera = FindObjectOfType<Camera>();
                    }

                    if (mainCamera != null)
                    {
                        ConfigureCanvasesForVR(mainCamera);
                    }
                }
            }
        }

        private static readonly string[] SkyboxSceneNames = new[]
        {
            "DarknessScene", "BaselineScene", "SafeRoomScene",
            "CrowdScene", "HeightScene", "FeedbackScene"
        };

        private bool IsSkyboxScene()
        {
            string current = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            foreach (var name in SkyboxSceneNames)
                if (current == name) return true;
            return false;
        }

        private void ConfigureCanvasesForVR(Camera mainCamera)
        {
            // ── Skybox/therapy scenes ──────────────────────────────────────────────────
            // These scenes render a 360° equirectangular panorama as a Unity skybox.
            // We must NOT convert their canvases to floating WorldSpace panels — that
            // covers the panorama entirely.  Instead we:
            //   1. Build the XR rig (so head tracking works).
            //   2. Switch the canvas to ScreenSpaceCamera mode so the skybox renders
            //      behind the canvas and is fully visible through any transparent areas.
            //   3. Force the camera clearFlags = Skybox.
            //   4. Add TrackedDeviceGraphicRaycaster so the Return button is still clickable.
            if (IsSkyboxScene())
            {
                VRRigBuilder.BuildVRRig(mainCamera, null);

                // Ensure the camera renders the skybox (not a flat colour)
                mainCamera.clearFlags = CameraClearFlags.Skybox;

                foreach (var canvas in FindObjectsOfType<Canvas>(true))
                {
                    if (canvas.transform.parent != null &&
                        canvas.transform.parent.GetComponent<SceneLoader>() != null)
                        continue;

                    // Switch to ScreenSpaceCamera so the skybox shows through
                    if (canvas.renderMode != RenderMode.ScreenSpaceCamera)
                    {
                        canvas.renderMode   = RenderMode.ScreenSpaceCamera;
                        canvas.worldCamera  = mainCamera;
                        canvas.planeDistance = 1.5f;   // 1.5 metres in front — comfortable viewing distance
                    }

                    // Keep XR raycaster so the Return button still works
                    if (canvas.GetComponent<TrackedDeviceGraphicRaycaster>() == null)
                        canvas.gameObject.AddComponent<TrackedDeviceGraphicRaycaster>();
                }
                return;
            }
            // Destroy legacy VRGazePointer to avoid conflicts and incorrect laser visual lines
            foreach (var gazePointer in FindObjectsOfType<VRGazePointer>(true))
            {
                Destroy(gazePointer);
                Debug.Log("[UIThemeAutoApply] Destroyed legacy VRGazePointer to prevent laser offset and interaction conflicts.");
            }

            Canvas mainCanvas = null;

            foreach (var canvas in FindObjectsOfType<Canvas>(true))
            {
                // Don't modify the SceneLoader transition canvas which is nested under SceneLoader
                if (canvas.transform.parent != null && canvas.transform.parent.GetComponent<SceneLoader>() != null)
                    continue;

                ConfigureCanvasForXR(canvas, mainCamera);

                if (canvas.renderMode == RenderMode.WorldSpace)
                {
                    mainCanvas = canvas;
                }
            }

            // Build the native XR Origin / Controller Rig
            VRRigBuilder.BuildVRRig(mainCamera, mainCanvas);

            // Find all input fields in the active scene and attach VRKeyboardTrigger
            AttachKeyboardTriggers();
        }

        private void ConfigureCanvasForXR(Canvas canvas, Camera mainCamera)
        {
            if (canvas == null) return;

            // If the canvas is screen space overlay, convert it to world space for VR visibility and targeting
            if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                canvas.renderMode = RenderMode.WorldSpace;
                canvas.worldCamera = mainCamera;
                
                // Position the canvas 1.5 meters in front of the camera, facing it
                canvas.transform.position = mainCamera.transform.position + mainCamera.transform.forward * 1.5f;
                canvas.transform.rotation = Quaternion.LookRotation(mainCamera.transform.forward, Vector3.up);
                
                // Scale the canvas down so 1920 pixels equals 1.92 meters wide in VR space
                canvas.transform.localScale = new Vector3(0.001f, 0.001f, 0.001f);
                
                Debug.Log($"[UIThemeAutoApply] Converted Canvas '{canvas.name}' to WorldSpace for VR compatibility.");
            }

            // Replace standard GraphicRaycaster with TrackedDeviceGraphicRaycaster for VR pointer interaction
            var oldRaycaster = canvas.GetComponent<GraphicRaycaster>();
            if (oldRaycaster != null)
            {
                Destroy(oldRaycaster);
            }

            if (canvas.GetComponent<TrackedDeviceGraphicRaycaster>() == null)
            {
                canvas.gameObject.AddComponent<TrackedDeviceGraphicRaycaster>();
                Debug.Log($"[UIThemeAutoApply] Added TrackedDeviceGraphicRaycaster to Canvas: {canvas.name}");
            }
        }

        private void AttachKeyboardTriggers()
        {
            foreach (var inputField in FindObjectsOfType<InputField>(true))
            {
                if (inputField.GetComponent<VRKeyboardTrigger>() == null)
                {
                    inputField.gameObject.AddComponent<VRKeyboardTrigger>();
                    Debug.Log($"[UIThemeAutoApply] Attached VRKeyboardTrigger to InputField: {inputField.name}");
                }
            }

            foreach (var tmpInputField in FindObjectsOfType<TMP_InputField>(true))
            {
                if (tmpInputField.GetComponent<VRKeyboardTrigger>() == null)
                {
                    tmpInputField.gameObject.AddComponent<VRKeyboardTrigger>();
                    Debug.Log($"[UIThemeAutoApply] Attached VRKeyboardTrigger to TMP_InputField: {tmpInputField.name}");
                }
            }
        }

        /// <summary>Returns true if the component lives inside the VRKeyboard canvas so we never theme it.</summary>
        private bool IsVRKeyboardObject(Component c)
        {
            Transform t = c.transform;
            while (t != null)
            {
                if (t.name == VR.VRKeyboard.KEYBOARD_ROOT_NAME) return true;
                t = t.parent;
            }
            return false;
        }

        private void ApplyThemeToImages(ThemePreset theme)
        {
            foreach (var image in FindObjectsOfType<Image>(true))
            {
                if (IsVRKeyboardObject(image)) continue;   // never theme keyboard buttons

                ThemeableUI themeable = image.GetComponent<ThemeableUI>();
                if (themeable == null)
                    themeable = image.gameObject.AddComponent<ThemeableUI>();

                themeable.elementType = ResolveImageElementType(image);
                themeable.ApplyTheme();
            }
        }

        private void ApplyThemeToTexts(ThemePreset theme)
        {
            foreach (var text in FindObjectsOfType<TextMeshProUGUI>(true))
            {
                if (IsVRKeyboardObject(text)) continue;    // never theme keyboard labels

                ThemeableUI themeable = text.GetComponent<ThemeableUI>();
                if (themeable == null)
                    themeable = text.gameObject.AddComponent<ThemeableUI>();

                themeable.elementType = ResolveTextElementType(text);
                themeable.ApplyTheme();
            }
        }

        private UIElementType ResolveImageElementType(Image image)
        {
            string name = image.gameObject.name.ToLower();

            if (image.GetComponent<Button>() != null)
            {
                return name.Contains("secondary")
                    ? UIElementType.SecondaryButton
                    : UIElementType.PrimaryButton;
            }

            if (image.GetComponentInParent<TMP_InputField>() != null)
            {
                return UIElementType.InputField;
            }

            if (name.Contains("card") || name.Contains("panel"))
                return UIElementType.CardBackground;

            if (name.Contains("background") || name.Contains("screen"))
                return UIElementType.ScreenBackground;

            return UIElementType.CardBackground;
        }

        private UIElementType ResolveTextElementType(TextMeshProUGUI text)
        {
            string name = text.gameObject.name.ToLower();

            if (name.Contains("error"))
                return UIElementType.ErrorText;

            if (text.GetComponentInParent<Button>() != null)
                return UIElementType.ButtonText;

            if (name.Contains("title") || name.Contains("heading") || text.fontSize >= 30)
                return UIElementType.HeadingText;

            if (name.Contains("subtitle") || text.fontSize >= 20)
                return UIElementType.SubheadingText;

            if (name.Contains("placeholder"))
                return UIElementType.PlaceholderText;

            if (name.Contains("label"))
                return UIElementType.LabelText;

            return UIElementType.BodyText;
        }
    }
}
