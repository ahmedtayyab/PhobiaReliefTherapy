using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using PhobiaReliefTherapy.Managers;
using PhobiaReliefTherapy.UI;
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
            VRMenuDragger.ReleaseAllGrabs();
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
            return VrAvailability.IsHeadsetPresent();
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
            if (IsSkyboxScene())
                mainCamera.clearFlags = CameraClearFlags.Skybox;

            foreach (var gazePointer in FindObjectsOfType<VRGazePointer>(true))
                Destroy(gazePointer);

            Canvas mainCanvas = null;
            foreach (var canvas in FindObjectsOfType<Canvas>(true))
            {
                if (IsProtectedCanvas(canvas))
                    continue;

                VrWorldMenu.AttachAndPlace(canvas, mainCamera, recenterIfLoose: true);
                if (canvas.renderMode == RenderMode.WorldSpace)
                    mainCanvas = canvas;
            }

            VRRigBuilder.BuildVRRig(mainCamera, mainCanvas);
            AttachKeyboardTriggers();
        }

        private static bool IsProtectedCanvas(Canvas canvas)
        {
            if (canvas == null)
                return true;
            if (canvas.transform.parent != null && canvas.transform.parent.GetComponent<SceneLoader>() != null)
                return true;
            if (canvas.name == "VitalsOverlayCanvas" || canvas.name == "TransitionCanvas"
                || canvas.name == VRKeyboard.KEYBOARD_ROOT_NAME)
                return true;
            if (canvas.GetComponentInParent<VRKeyboard>() != null)
                return true;
            return canvas.GetComponentInParent<SessionVitalsOverlay>() != null;
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
                if (themeable.elementType == UIElementType.ButtonText)
                    ClinicalUIFactory.AlignButtonLabel(text);
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
