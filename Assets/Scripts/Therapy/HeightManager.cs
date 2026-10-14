using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using PhobiaReliefTherapy.Managers;
using PhobiaReliefTherapy.Theme;
using PhobiaReliefTherapy.Data;
using PhobiaReliefTherapy;

namespace PhobiaReliefTherapy.Therapy
{
    public class HeightManager : MonoBehaviour
    {
        [Header("UI Elements")]
        public TextMeshProUGUI timerText;
        public Button returnButton;

        [Header("Skybox Setup")]
        public Material heightSkyboxMaterial;

        [Header("Settings")]
        public float exposureDuration = 120f;

        private Texture2D heightTexture;
        private Sprite heightSprite;
        private Material originalSkyboxMaterial;
        private bool hasAppliedSkybox = false;

        private GameObject customBackgroundGO;
        private Quaternion originalCameraRotation;
        private CameraClearFlags originalCameraClearFlags;
        private float yaw = 0f;
        private float pitch = 0f;
        private bool isTimerRunning = false;

        private bool sessionEnded = false;
        private Transform panelTransform;

        private void Start()
        {
            Debug.Log("HeightManager component started.");
            VRUIInputBridge.EnsureInstanceExists();
            VRLocomotionBridge.EnsureInstanceExists();
            AutoBindMissingFields();

            VRManager.EnsureInstanceExists();
            VRManager.Instance.InitializeVR();

            InitializeHeightEnvironment();

            if (returnButton != null)
            {
                returnButton.onClick.RemoveAllListeners();
                returnButton.onClick.AddListener(() => {
                    if (sessionEnded) return;
                    sessionEnded = true;
                    Debug.Log("Return button clicked. Ending session.");
                    TherapySessionHelper.CompleteExposureSession(true);
                });
            }

            WireEmergencyStop();
            AdaptiveExposureController.StageChanged += OnAdaptiveStage;
            StartExposureTherapy();
        }

        private void OnAdaptiveStage(int stage)
        {
            ApplyStageTexture(Mathf.Clamp(stage, 1, 3));
            TherapySessionHelper.ApplyDifficultyToSkyboxMaterial(RenderSettings.skybox);
        }

        private void WireEmergencyStop()
        {
            if (panelTransform == null)
            {
                var canvas = Object.FindObjectOfType<Canvas>();
                if (canvas != null)
                {
                    var panel = canvas.transform.Find("Panel");
                    if (panel != null)
                        panelTransform = panel;
                }
            }

            var emergencyButton = TherapySessionHelper.CreateEmergencyStopButton(panelTransform);
            TherapySessionHelper.WireEmergencyStop(emergencyButton, () =>
            {
                if (sessionEnded) return;
                sessionEnded = true;
                TherapySessionHelper.AbortToSafeRoom(true);
            });
        }

        private void OnSafetyTriggered(PanicDetectionService.PanicEvaluation evaluation)
        {
            if (sessionEnded) return;
            sessionEnded = true;
            Debug.Log($"Safety triggered: {evaluation.Classification} - {evaluation.AiRecommendation}");
            TherapySessionHelper.AbortToSafeRoom(true);
        }

        private void Update()
        {
            if (isTimerRunning)
            {
#if UNITY_ANDROID && !UNITY_EDITOR
                // On Quest: always hide flat 2D background, let the skybox + HMD rotation do the work
                if (customBackgroundGO != null && customBackgroundGO.activeSelf)
                    customBackgroundGO.SetActive(false);
#else
                if (IsVRActive())
                {
                    if (customBackgroundGO != null && customBackgroundGO.activeSelf)
                        customBackgroundGO.SetActive(false);
                }
                else
                {
                    SimulateEditor3DLook();
                }
#endif
            }
        }

        private void SimulateEditor3DLook()
        {
            if (Input.GetMouseButton(1)) // Hold Right Mouse Button
            {
                if (customBackgroundGO != null && customBackgroundGO.activeSelf)
                {
                    customBackgroundGO.SetActive(false); // Hide 2D background to reveal 3D Skybox
                }

                if (Camera.main != null)
                {
                    yaw += Input.GetAxis("Mouse X") * 2f;
                    pitch -= Input.GetAxis("Mouse Y") * 2f;
                    pitch = Mathf.Clamp(pitch, -80f, 80f);
                    Camera.main.transform.localEulerAngles = new Vector3(pitch, yaw, 0f);
                }
            }
            else
            {
                if (customBackgroundGO != null && !customBackgroundGO.activeSelf)
                {
                    customBackgroundGO.SetActive(true); // Restore 2D fallback flat background
                    if (Camera.main != null)
                    {
                        Camera.main.transform.localRotation = originalCameraRotation;
                    }
                }
            }
        }

        private void AutoBindMissingFields()
        {
            if (timerText == null)
                timerText = AutoBindField<TextMeshProUGUI>("TimerText");
            if (returnButton == null)
                returnButton = AutoBindField<Button>("ReturnButton");
        }

        private T AutoBindField<T>(string objectName) where T : Component
        {
            T result = AutoBindHelper.FindComponentInChildrenByName<T>(transform, objectName);
            return result != null ? result : AutoBindHelper.FindComponentByName<T>(objectName);
        }

        private Texture2D LoadHeightTextureForStage(int stage)
        {
            string baseName = "height_image";
            if (stage == 2) baseName = "height_image2";
            if (stage == 3) baseName = "height_image3";

            Debug.Log($"Loading height texture for stage {stage} (resource: {baseName})...");

            Texture2D tex = Resources.Load<Texture2D>(baseName);
            if (tex != null) { Debug.Log($"Loaded '{baseName}' from Resources."); return tex; }

            Sprite sprite = Resources.Load<Sprite>(baseName);
            if (sprite != null && sprite.texture != null) { Debug.Log($"Loaded '{baseName}' sprite from Resources."); return sprite.texture; }

            if (stage == 1)
            {
                tex = Resources.Load<Texture2D>("height");
                if (tex != null) { Debug.Log("Loaded 'height' from Resources."); return tex; }

                sprite = Resources.Load<Sprite>("height");
                if (sprite != null && sprite.texture != null) { Debug.Log("Loaded 'height' sprite from Resources."); return sprite.texture; }

                tex = LoadTextureFromFileSystem("Resources/height_image.png");
                if (tex != null) { Debug.Log("Loaded 'Resources/height_image.png' from fallback."); return tex; }

                tex = LoadTextureFromFileSystem("Assets/Resources/height_image.png");
                if (tex != null) { Debug.Log("Loaded 'Assets/Resources/height_image.png' from fallback."); return tex; }
            }
            else
            {
                string suffix = stage == 2 ? "2" : "3";
                tex = LoadTextureFromFileSystem($"Resources/height_image{suffix}.jpeg");
                if (tex != null) return tex;
                tex = LoadTextureFromFileSystem($"Assets/Resources/height_image{suffix}.jpeg");
                if (tex != null) return tex;
            }

            Debug.LogError($"Failed to load height texture for stage {stage}!");
            return null;
        }

        private void ApplyStageTexture(int stage)
        {
            Texture2D tex = LoadHeightTextureForStage(stage);
            if (tex != null)
            {
                heightTexture = tex;

                if (RenderSettings.skybox != null)
                {
                    RenderSettings.skybox.SetTexture("_MainTex", heightTexture);
                    DynamicGI.UpdateEnvironment();
                    Debug.Log($"Applied Stage {stage} 3D Skybox texture.");
                }

                if (customBackgroundGO != null)
                {
                    Image bgImgComponent = customBackgroundGO.GetComponent<Image>();
                    if (bgImgComponent != null)
                    {
                        if (heightSprite != null)
                        {
                            Destroy(heightSprite);
                        }
                        heightSprite = Sprite.Create(heightTexture, new Rect(0, 0, heightTexture.width, heightTexture.height), new Vector2(0.5f, 0.5f));
                        bgImgComponent.sprite = heightSprite;
                        bgImgComponent.color = Color.white;
                        Debug.Log($"Applied Stage {stage} 2D Fallback background sprite.");
                    }
                }
            }
        }

        private Texture2D LoadTextureFromFileSystem(string relativePath)
        {
            try
            {
                string fullPath = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, relativePath));
                if (System.IO.File.Exists(fullPath))
                {
                    byte[] fileData = System.IO.File.ReadAllBytes(fullPath);
                    Texture2D texture = new Texture2D(2, 2);
                    if (texture.LoadImage(fileData))
                    {
                        return texture;
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Error loading texture directly from filesystem: {ex.Message}");
            }
            return null;
        }

        private Shader FindSkyboxShader()
        {
            Shader shader = Shader.Find("Skybox/Panoramic");
            if (shader == null) shader = Shader.Find("Legacy Shaders/Skybox/Panoramic");
            if (shader == null) shader = Shader.Find("Skybox/6 Sided");
            if (shader == null) shader = Shader.Find("Internal-SkyboxOnGPU");
            return shader;
        }

        private void InitializeHeightEnvironment()
        {
            // Find the correct canvas belonging to this specific scene (to prevent picking up other additive canvases)
            Canvas canvas = null;
            var canvases = Object.FindObjectsOfType<Canvas>();
            foreach (var c in canvases)
            {
                if (c.gameObject.scene == gameObject.scene)
                {
                    canvas = c;
                    break;
                }
            }
            if (canvas == null)
            {
                canvas = Object.FindObjectOfType<Canvas>();
            }

            if (canvas == null)
            {
                Debug.LogError("No Canvas found in the scene during InitializeHeightEnvironment!");
                return;
            }

            // Disable original Panel Image component if it exists
            GameObject panelGO = null;
            var panelTransformLocal = canvas.transform.Find("Panel");
            if (panelTransformLocal != null)
            {
                panelGO = panelTransformLocal.gameObject;
                panelTransform = panelTransformLocal;
                var panelImage = panelTransformLocal.GetComponent<Image>();
                if (panelImage != null)
                {
                    panelImage.enabled = false;
                    panelImage.raycastTarget = false; // Prevent blocking mouse look drags
                }
            }
            else
            {
                // Create Panel container if not exists
                panelGO = new GameObject("Panel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                panelGO.transform.SetParent(canvas.transform, false);
                panelTransform = panelGO.transform;
                RectTransform panelRect = panelGO.GetComponent<RectTransform>();
                panelRect.anchorMin = Vector2.zero;
                panelRect.anchorMax = Vector2.one;
                panelRect.offsetMin = Vector2.zero;
                panelRect.offsetMax = Vector2.zero;
                
                var img = panelGO.GetComponent<Image>();
                img.color = new Color(0, 0, 0, 0);
                img.raycastTarget = false;
            }

            heightTexture = LoadHeightTextureForStage(1);

            // 1. 3D Panoramic Skybox setup
            Material skyboxMat = heightSkyboxMaterial;
            if (skyboxMat == null)
            {
                Debug.LogWarning("heightSkyboxMaterial not pre-assigned. Attempting fallback creation.");
                Shader skyboxShader = FindSkyboxShader();
                if (skyboxShader != null)
                {
                    skyboxMat = new Material(skyboxShader);
                }
            }

            if (skyboxMat != null)
            {
                originalSkyboxMaterial = RenderSettings.skybox;
                if (heightTexture != null)
                {
                    skyboxMat.SetTexture("_MainTex", heightTexture);
                }
                skyboxMat.SetFloat("_Mapping", 1.0f);    // Latitude Longitude
                skyboxMat.SetFloat("_ImageType", 0.0f);  // 360 Degrees
                skyboxMat.SetFloat("_Exposure", 1.0f);

                RenderSettings.skybox = skyboxMat;
                DynamicGI.UpdateEnvironment();
                hasAppliedSkybox = true;
                TherapySessionHelper.ApplyDifficultyToSkyboxMaterial(skyboxMat);
                Debug.Log("Applied 3D Panoramic Skybox successfully.");
            }
            else
            {
                Debug.LogError("Panoramic Skybox material/shader could not be instantiated!");
            }

            // 2. 2D Fallback Background Setup
            if (customBackgroundGO == null)
            {
                customBackgroundGO = new GameObject("HeightBackground2D", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                customBackgroundGO.transform.SetParent(canvas.transform, false);
                customBackgroundGO.transform.SetAsFirstSibling();

                Image bgImgComponent = customBackgroundGO.GetComponent<Image>();
                bgImgComponent.raycastTarget = false; // Crucial: prevent this full-screen image from blocking camera look drag clicks!

                if (heightTexture != null)
                {
                    heightSprite = Sprite.Create(heightTexture, new Rect(0, 0, heightTexture.width, heightTexture.height), new Vector2(0.5f, 0.5f));
                    bgImgComponent.sprite = heightSprite;
                    bgImgComponent.color = Color.white;
                }

                RectTransform bgRect = customBackgroundGO.GetComponent<RectTransform>();
                bgRect.anchorMin = Vector2.zero;
                bgRect.anchorMax = Vector2.one;
                bgRect.offsetMin = Vector2.zero;
                bgRect.offsetMax = Vector2.zero;
                
                // Immediately deactivate the 2D background if VR is active to reveal the skybox instantly
                if (IsVRActive())
                {
                    customBackgroundGO.SetActive(false);
                }
                
                Debug.Log("Created 2D Fallback Background Image.");
            }

            // 3. Runtime fallback UI creation if fields are not bound (i.e. empty scene fallback)
            if (timerText == null || returnButton == null)
            {
                Debug.Log("Scene elements missing at runtime. Spawning minimal corner UI dynamically.");

                if (timerText == null)
                {
                    GameObject timerGO = new GameObject("TimerText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
                    timerGO.transform.SetParent(panelGO.transform, false);

                    RectTransform timerRect = timerGO.GetComponent<RectTransform>();
                    timerRect.anchorMin = new Vector2(1f, 1f);
                    timerRect.anchorMax = new Vector2(1f, 1f);
                    timerRect.pivot = new Vector2(1f, 1f);
                    timerRect.anchoredPosition = new Vector2(-50f, -50f);
                    timerRect.sizeDelta = new Vector2(300f, 80f);

                    timerText = timerGO.GetComponent<TextMeshProUGUI>();
                    timerText.text = $"{SessionTiming.ExposureSeconds:0}s";
                    timerText.alignment = TextAlignmentOptions.TopRight;

                    ThemeableUI timerTheme = timerGO.AddComponent<ThemeableUI>();
                    timerTheme.elementType = UIElementType.HeadingText;
                    timerTheme.ApplyTheme();
                }

                if (returnButton == null)
                {
                    GameObject buttonGO = new GameObject("ReturnButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
                    buttonGO.transform.SetParent(panelGO.transform, false);

                    RectTransform btnRect = buttonGO.GetComponent<RectTransform>();
                    btnRect.anchorMin = new Vector2(0f, 1f);
                    btnRect.anchorMax = new Vector2(0f, 1f);
                    btnRect.pivot = new Vector2(0f, 1f);
                    btnRect.anchoredPosition = new Vector2(50f, -50f);
                    btnRect.sizeDelta = new Vector2(220f, 55f);

                    returnButton = buttonGO.GetComponent<Button>();
                    ThemeableUI btnTheme = buttonGO.AddComponent<ThemeableUI>();
                    btnTheme.elementType = UIElementType.PrimaryButton;

                    GameObject btnTextGO = new GameObject("ButtonText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
                    btnTextGO.transform.SetParent(buttonGO.transform, false);

                    RectTransform btnTextRect = btnTextGO.GetComponent<RectTransform>();
                    btnTextRect.anchorMin = Vector2.zero;
                    btnTextRect.anchorMax = Vector2.one;
                    btnTextRect.offsetMin = Vector2.zero;
                    btnTextRect.offsetMax = Vector2.zero;

                    TextMeshProUGUI btnText = btnTextGO.GetComponent<TextMeshProUGUI>();
                    btnText.text = "Return to Menu";
                    btnText.alignment = TextAlignmentOptions.Center;

                    ThemeableUI btnTextTheme = btnTextGO.AddComponent<ThemeableUI>();
                    btnTextTheme.elementType = UIElementType.ButtonText;

                    btnTheme.ApplyTheme();
                    btnTextTheme.ApplyTheme();
                }
            }

            if (IsVRActive())
            {
                if (timerText != null)
                {
                    RectTransform rect = timerText.rectTransform;
                    rect.anchorMin = new Vector2(0.75f, 0.85f);
                    rect.anchorMax = new Vector2(0.75f, 0.85f);
                    rect.pivot = new Vector2(0.5f, 0.5f);
                    rect.anchoredPosition = Vector2.zero;
                    timerText.alignment = TextAlignmentOptions.Center;
                }

                if (returnButton != null)
                {
                    RectTransform rect = returnButton.GetComponent<RectTransform>();
                    rect.anchorMin = new Vector2(0.5f, 0.25f);
                    rect.anchorMax = new Vector2(0.5f, 0.25f);
                    rect.pivot = new Vector2(0.5f, 0.5f);
                    rect.anchoredPosition = Vector2.zero;
                }
            }
        }

        private void StartExposureTherapy()
        {
            if (Camera.main != null)
            {
                originalCameraRotation = Camera.main.transform.localRotation;
                originalCameraClearFlags = Camera.main.clearFlags;
                Camera.main.clearFlags = CameraClearFlags.Skybox;

                yaw = originalCameraRotation.eulerAngles.y;
                pitch = originalCameraRotation.eulerAngles.x;
                Debug.Log("Initialized camera clear flags to Skybox.");
            }

            TherapySessionHelper.BeginExposureSession(OnSafetyTriggered);
            StartCoroutine(ExposureTimerRoutine());
        }

        private IEnumerator ExposureTimerRoutine()
        {
            isTimerRunning = true;
            int stage = TherapySessionHelper.GetAssignedExposureStage();
            UserData.CurrentStage = stage;

            ApplyStageTexture(stage);
            TherapySessionHelper.ApplyDifficultyToSkyboxMaterial(RenderSettings.skybox);

            float remaining = SessionTiming.ExposureSeconds;
            Debug.Log($"Starting AI-assigned exposure stage {stage}.");

            while (remaining > 0)
            {
                if (sessionEnded || !SessionMetrics.SessionActive)
                    yield break;

                if (SessionTiming.ConsumeSkip())
                    remaining = 0f;

                stage = UserData.CurrentStage;
                if (timerText != null)
                    timerText.text = $"S{stage} " + Mathf.CeilToInt(remaining).ToString() + "s";

                yield return new WaitForSeconds(1f);
                remaining -= 1f;
            }

            if (timerText != null)
                timerText.text = "Session Complete!";

            isTimerRunning = false;

            if (!sessionEnded)
            {
                sessionEnded = true;
                TherapySessionHelper.CompleteExposureSession(false);
            }
        }

        private void OnDestroy()
        {
            AdaptiveExposureController.StageChanged -= OnAdaptiveStage;
            Debug.Log("HeightManager component destroyed. Restoring environment.");
            // Restore camera clear flags and rotation
            if (Camera.main != null)
            {
                Camera.main.transform.localRotation = originalCameraRotation;
                Camera.main.clearFlags = originalCameraClearFlags;
            }

            // Restore skybox
            if (hasAppliedSkybox && originalSkyboxMaterial != null)
            {
                RenderSettings.skybox = originalSkyboxMaterial;
                DynamicGI.UpdateEnvironment();
            }

            // Clean up custom textures/sprites if they were created at runtime
            if (heightSprite != null)
            {
                Destroy(heightSprite);
            }
        }

        private bool IsVRActive()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            return true; // Always force VR mode on Oculus Quest standalone builds
#else
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
    }

    /// <summary>
    /// Bootstrapper to automatically instantiate HeightManager in the HeightScene on scene transitions.
    /// </summary>
    public static class HeightManagerBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void Initialize()
        {
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private static void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
        {
            if (scene.name == "HeightScene")
            {
                // Ensure a HeightManager is present in the scene
                if (Object.FindObjectOfType<HeightManager>() == null)
                {
                    GameObject managerGO = new GameObject("HeightManager");
                    managerGO.AddComponent<HeightManager>();
                    Debug.Log("Bootstrapped HeightManager dynamically on scene loaded event.");
                }
            }
        }
    }
}
