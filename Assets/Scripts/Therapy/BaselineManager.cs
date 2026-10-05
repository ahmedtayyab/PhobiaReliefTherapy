using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using PhobiaReliefTherapy.Data;
using PhobiaReliefTherapy.Managers;
using PhobiaReliefTherapy.Theme;
using PhobiaReliefTherapy.UI;

namespace PhobiaReliefTherapy.Therapy
{
    /// <summary>
    /// Measures baseline vitals (simulated) before entering the Safe Room.
    /// Supports a hybrid 2D/3D setup for perfect presentation on both VR headsets and flat screens.
    /// </summary>
    public class BaselineManager : MonoBehaviour
    {
        [Header("UI Elements")]
        public TextMeshProUGUI instructionText;
        public TextMeshProUGUI timerText;
        public TextMeshProUGUI resultText;
        public TextMeshProUGUI stageText;
        public TextMeshProUGUI sensorModeText;
        public Image progressBarFill;
        public Button continueButton;

        public float measurementDuration = 60f;

        [Header("Skybox Setup")]
        public Material baselineSkyboxMaterial;

        private Material originalSkyboxMaterial;
        private bool hasAppliedSkybox = false;
        private GameObject customBackgroundGO;
        private List<GameObject> disabledCardPanels = new List<GameObject>();
        
        private Vector3 originalTimerPosition;
        private Transform originalTimerParent;

        // VR & Simulator controls
        private Quaternion originalCameraRotation;
        private CameraClearFlags originalCameraClearFlags;
        private float yaw = 0f;
        private float pitch = 0f;
        private bool isMeasuring = false;

        // Caching original layout properties of the instructionText
        private Transform originalInstructionParent;
        private Vector3 originalInstructionPosition;
        private Vector2 originalInstructionSizeDelta;
        private TextAlignmentOptions originalInstructionAlignment;
        private float originalInstructionFontSize;
        private FontStyles originalInstructionFontStyle;
        private Color originalInstructionColor;

        private void Start()
        {
            VRUIInputBridge.EnsureInstanceExists();
            VRLocomotionBridge.EnsureInstanceExists();
            AutoBindMissingFields();

            if (continueButton != null)
                continueButton.gameObject.SetActive(false);
            if (resultText != null)
                resultText.text = "";
            if (stageText != null)
                stageText.text = "Baseline Measurement";

            SensorManager.EnsureInstanceExists();
            VRManager.EnsureInstanceExists();
            VRManager.Instance.InitializeVR();

            UpdateSensorModeText();
            StartBaselineMeasurement();
        }

        private void Update()
        {
            if (isMeasuring)
            {
                UpdateSensorModeText();
                if (instructionText != null)
                    instructionText.text = "Remain still and calm.\n" + SensorManager.GetBaselineInstruction();
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
                    // If not in VR, allow Editor simulation to preview 3D environment look
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
            if (instructionText == null)
                instructionText = AutoBindField<TextMeshProUGUI>("InstructionText");
            if (timerText == null)
                timerText = AutoBindField<TextMeshProUGUI>("TimerText");
            if (resultText == null)
                resultText = AutoBindField<TextMeshProUGUI>("ResultText");
            if (stageText == null)
                stageText = AutoBindField<TextMeshProUGUI>("StageText");
            if (sensorModeText == null)
                sensorModeText = AutoBindField<TextMeshProUGUI>("SensorModeText");
            if (progressBarFill == null)
                progressBarFill = AutoBindField<Image>("ProgressBarFill");
            if (continueButton == null)
                continueButton = AutoBindField<Button>("ContinueButton");
        }

        private T AutoBindField<T>(string objectName) where T : Component
        {
            T result = AutoBindHelper.FindComponentInChildrenByName<T>(transform, objectName);
            return result != null ? result : AutoBindHelper.FindComponentByName<T>(objectName);
        }

        /// <summary>
        /// Loads a Texture2D directly from the file system, bypassing Unity's Asset Database lag.
        /// </summary>
        private Texture2D LoadTextureFromFileSystem(string relativePath)
        {
            try
            {
                string fullPath = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, relativePath));
                if (System.IO.File.Exists(fullPath))
                {
                    byte[] fileData = System.IO.File.ReadAllBytes(fullPath);
                    Texture2D texture = new Texture2D(2, 2);
                    if (texture.LoadImage(fileData)) // Automatically resizes texture to match dimensions
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

        private Texture2D LoadCalmTexture()
        {
            // 1. Try to load from Resources as Texture2D directly
            Texture2D tex = Resources.Load<Texture2D>("baseline_image");
            if (tex != null) return tex;

            // 2. Try to load from Resources as Sprite and extract texture
            Sprite sprite = Resources.Load<Sprite>("baseline_image");
            if (sprite != null && sprite.texture != null) return sprite.texture;

            // 3. Fallback: Load from filesystem (useful in custom Editor folders)
            tex = LoadTextureFromFileSystem("../images/baseline_image.jpeg");
            if (tex != null) return tex;

            tex = LoadTextureFromFileSystem("Resources/baseline_image.jpeg");
            return tex;
        }

        private Sprite LoadCalmSprite()
        {
            // 1. Try to load from Resources as Sprite directly
            Sprite sprite = Resources.Load<Sprite>("baseline_image");
            if (sprite != null) return sprite;

            // 2. Try to load as Texture2D and wrap it in a Sprite
            Texture2D tex = LoadCalmTexture();
            if (tex != null)
            {
                return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
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

        private void InitializeBaselineBackgroundAndLayout()
        {
            // Find the correct canvas containing the Panel/CardPanel hierarchy
            Canvas canvas = null;
            var allCanvases = Object.FindObjectsOfType<Canvas>();
            foreach (var c in allCanvases)
            {
                if (c != null && (c.transform.Find("Panel") != null || c.transform.Find("CardPanel") != null))
                {
                    canvas = c;
                    break;
                }
            }
            if (canvas == null)
            {
                canvas = Object.FindObjectOfType<Canvas>();
            }
            if (canvas == null) return;

            var panelTransform = canvas.transform.Find("Panel");

            // 1. 3D Panoramic Skybox setup: This binds the baseline_image as a 3D panorama
            // to allow full 360-degree head-tracking when wearing a VR headset!
            if (!hasAppliedSkybox)
            {
                Texture2D texture = LoadCalmTexture();

                if (texture != null)
                {
                    originalSkyboxMaterial = RenderSettings.skybox;
                    
                    // 1. Use the pre-assigned inspector material if available (highly recommended for standalone builds!)
                    Material skyboxMat = baselineSkyboxMaterial;
                    
                    // 2. Fallback: try loading from Resources
                    if (skyboxMat == null)
                    {
                        skyboxMat = Resources.Load<Material>("BaselineSkyboxMaterial");
                    }
                    
                    // 3. Fallback: runtime creation
                    if (skyboxMat == null)
                    {
                        Shader skyboxShader = FindSkyboxShader();
                        if (skyboxShader != null)
                        {
                            skyboxMat = new Material(skyboxShader);
                        }
                    }

                    if (skyboxMat != null)
                    {
                        // Map the 3D calm panorama texture to the skybox material
                        skyboxMat.SetTexture("_MainTex", texture);

                        // Explicitly configure modern panoramic properties so they render successfully in both URP and Built-in pipelines
                        skyboxMat.SetFloat("_Mapping", 1.0f);    // 1 = Latitude Longitude (Panoramic 360)
                        skyboxMat.SetFloat("_ImageType", 0.0f);  // 0 = 360 Degrees
                        skyboxMat.SetFloat("_Exposure", 1.0f);   // Ensure exposure is fully visible (not black)
                        
                        RenderSettings.skybox = skyboxMat;
                        DynamicGI.UpdateEnvironment();
                        hasAppliedSkybox = true;
                    }
                    else
                    {
                        Debug.LogWarning("Skybox material/shader not found in project. Falling back to 2D background.");
                    }
                }
                else
                {
                    Debug.LogWarning("Baseline image resource 'baseline_image' not found.");
                }
            }

            // 2. 2D Fallback Background Image setup: Ensures flat screen/editor mode ALWAYS displays the image instantly
            if (customBackgroundGO == null)
            {
                customBackgroundGO = new GameObject("BaselineBackground2D", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                customBackgroundGO.transform.SetParent(canvas.transform, false);
                customBackgroundGO.transform.SetAsFirstSibling(); // place it at the very back of the Canvas

                Image bgImgComponent = customBackgroundGO.GetComponent<Image>();
                Sprite sprite = LoadCalmSprite();

                if (sprite != null)
                {
                    bgImgComponent.sprite = sprite;
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
            }

            // Disable the original Panel Image component to reveal our backgrounds
            if (panelTransform != null)
            {
                var panelImage = panelTransform.GetComponent<Image>();
                if (panelImage != null) panelImage.enabled = false;
            }

            IgnoreLayout(timerText != null ? timerText.gameObject : null);
            IgnoreLayout(progressBarFill != null ? progressBarFill.gameObject : null);
            if (progressBarFill != null && progressBarFill.transform.parent != null)
                IgnoreLayout(progressBarFill.transform.parent.gameObject);

            var panelLayout = panelTransform != null ? panelTransform.GetComponent<VerticalLayoutGroup>() : null;
            if (panelLayout != null)
                panelLayout.enabled = false;
            var canvasLayout = canvas.GetComponent<VerticalLayoutGroup>();
            if (canvasLayout != null)
                canvasLayout.enabled = false;

            LayoutHorizontalTimer(panelTransform != null ? panelTransform : canvas.transform);

            // Move instructionText to a corner (Bottom-Left corner of Canvas Panel, or center-bottom if VR is active)
            if (instructionText != null)
            {
                originalInstructionParent = instructionText.transform.parent;
                originalInstructionPosition = instructionText.transform.localPosition;
                originalInstructionSizeDelta = instructionText.rectTransform.sizeDelta;
                originalInstructionAlignment = instructionText.alignment;
                originalInstructionFontSize = instructionText.fontSize;
                originalInstructionFontStyle = instructionText.fontStyle;
                originalInstructionColor = instructionText.color;

                // Move instructionText to be a direct child of Panel or Canvas to float freely
                IgnoreLayout(instructionText.gameObject);
                instructionText.transform.SetParent(panelTransform != null ? panelTransform : canvas.transform, true);

                RectTransform rect = instructionText.rectTransform;
                if (IsVRActive())
                {
                    rect.anchorMin = new Vector2(0.5f, 0.2f);
                    rect.anchorMax = new Vector2(0.5f, 0.2f);
                    rect.pivot = new Vector2(0.5f, 0.5f);
                    rect.anchoredPosition = Vector2.zero;
                    rect.sizeDelta = new Vector2(700f, 150f);
                    instructionText.alignment = TextAlignmentOptions.Center;
                }
                else
                {
                    rect.anchorMin = new Vector2(0, 0); // Bottom-Left anchor
                    rect.anchorMax = new Vector2(0, 0);
                    rect.pivot = new Vector2(0, 0);
                    rect.anchoredPosition = new Vector2(50f, 50f); // Bottom-left offset
                    rect.sizeDelta = new Vector2(700f, 150f); // Set clear bounds for wrap layout
                    instructionText.alignment = TextAlignmentOptions.BottomLeft;
                }
                instructionText.fontSize = 24f;
                instructionText.fontStyle = FontStyles.Normal;
                instructionText.color = new Color(0.95f, 0.95f, 0.95f, 0.95f); // Super crisp readable white
            }
        }

        private static void IgnoreLayout(GameObject go)
        {
            if (go == null)
                return;
            var layout = go.GetComponent<LayoutElement>();
            if (layout == null)
                layout = go.AddComponent<LayoutElement>();
            layout.ignoreLayout = true;
        }

        private void LayoutHorizontalTimer(Transform parent)
        {
            if (timerText != null)
            {
                originalTimerParent = timerText.transform.parent;
                originalTimerPosition = timerText.transform.localPosition;
                timerText.transform.SetParent(parent, false);

                RectTransform rect = timerText.rectTransform;
                rect.anchorMin = new Vector2(0.58f, 0.88f);
                rect.anchorMax = new Vector2(0.94f, 0.88f);
                rect.pivot = new Vector2(1f, 0.5f);
                rect.anchoredPosition = Vector2.zero;
                rect.sizeDelta = new Vector2(0f, 56f);
                timerText.alignment = TextAlignmentOptions.MidlineRight;
                timerText.enableWordWrapping = false;
                timerText.overflowMode = TextOverflowModes.Overflow;
                timerText.fontSize = 36f;
                timerText.fontStyle = FontStyles.Bold;
                timerText.color = Color.white;
            }

            if (progressBarFill == null)
                return;

            progressBarFill.type = Image.Type.Filled;
            progressBarFill.fillMethod = Image.FillMethod.Horizontal;
            progressBarFill.fillOrigin = (int)Image.OriginHorizontal.Left;
            progressBarFill.fillAmount = 0f;
            progressBarFill.gameObject.SetActive(true);

            var bar = progressBarFill.rectTransform;
            if (bar.parent != null && bar.parent != parent && bar.parent.GetComponent<Slider>() != null)
            {
                var slider = bar.parent.GetComponent<RectTransform>();
                slider.SetParent(parent, false);
                StretchHorizontalBar(slider);
                var sliderComp = slider.GetComponent<Slider>();
                if (sliderComp != null)
                    sliderComp.direction = Slider.Direction.LeftToRight;
            }
            else
            {
                bar.SetParent(parent, false);
                StretchHorizontalBar(bar);
            }
        }

        private static void StretchHorizontalBar(RectTransform rect)
        {
            rect.anchorMin = new Vector2(0.08f, 0.88f);
            rect.anchorMax = new Vector2(0.56f, 0.88f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(0f, 18f);
            rect.localRotation = Quaternion.identity;
            rect.localScale = Vector3.one;
        }

        private void StartBaselineMeasurement()
        {
            InitializeBaselineBackgroundAndLayout();

            isMeasuring = true;
            if (Camera.main != null)
            {
                originalCameraRotation = Camera.main.transform.localRotation;
                originalCameraClearFlags = Camera.main.clearFlags;
                
                // Force camera clear flags to Skybox so it correctly renders skyboxes at runtime
                Camera.main.clearFlags = CameraClearFlags.Skybox;
                
                yaw = originalCameraRotation.eulerAngles.y;
                pitch = originalCameraRotation.eulerAngles.x;
            }

            // Temporarily set all CardPanel GameObjects in the scene to inactive so they are 100% hidden
            disabledCardPanels.Clear();
            var allObjects = Object.FindObjectsOfType<GameObject>();
            foreach (var go in allObjects)
            {
                if (go != null)
                {
                    if (go.name.Contains("CardPanel"))
                    {
                        go.SetActive(false);
                        disabledCardPanels.Add(go);
                    }
                    else if (go.name == "Panel")
                    {
                        var img = go.GetComponent<Image>();
                        if (img != null) img.enabled = false;
                    }
                }
            }

            SessionVitalsHistory.Clear();
            instructionText.text = "Remain still and calm.\n" + SensorManager.GetBaselineInstruction();
            measurementDuration = SessionTiming.BaselineSeconds;
            SensorManager.Instance.InitializeSensor(() =>
            {
                UpdateSensorModeText();
                if (instructionText != null)
                    instructionText.text = "Remain still and calm.\n" + SensorManager.GetBaselineInstruction();
                SensorManager.Instance.StartBaselineMeasurement(measurementDuration, OnBaselineComplete);
                StartCoroutine(BaselineTimerRoutine());
            });
            EnsureSkipButton();
        }

        private void UpdateSensorModeText()
        {
            if (sensorModeText == null || SensorManager.Instance == null)
                return;

            int bpm = SensorManager.Instance.CurrentHeartRate;
            sensorModeText.text = $"Sensor: {SensorManager.Instance.SensorSourceName} ({bpm} BPM)";
        }

        private IEnumerator BaselineTimerRoutine()
        {
            HeadMovementTracker.EnsureInstanceExists();
            float timer = measurementDuration;
            while (timer > 0)
            {
                if (SessionTiming.ConsumeSkip())
                    timer = 0f;

                if (HeadMovementTracker.Instance != null)
                    SessionMetrics.RecordHeadMovementSample(HeadMovementTracker.Instance.AgitationScore);

                float progress = 1f - (timer / measurementDuration);
                if (timerText != null)
                    timerText.text = Mathf.CeilToInt(timer).ToString() + "s";
                if (progressBarFill != null)
                    progressBarFill.fillAmount = Mathf.Clamp01(progress);

                yield return new WaitForSeconds(1f);
                timer -= 1f;
            }

            if (timerText != null)
                timerText.text = "0s";
            if (progressBarFill != null)
                progressBarFill.fillAmount = 1f;
        }

        private void OnBaselineComplete(int heartRate)
        {
            isMeasuring = false;
            UserData.BaselineHeartRate = heartRate;

            HeadMovementTracker.EnsureInstanceExists();
            ExposureSessionMonitor.EnsureInstanceExists();
            if (ExposureSessionMonitor.Instance != null)
                ExposureSessionMonitor.Instance.CaptureBaselineHeadMovement();

            ExposureStageAdvisor.AssignStageFromBaseline(heartRate, SessionMetrics.BaselineHeadMovementScore);

            // Snap camera back to original rotation and clear flags
            if (Camera.main != null)
            {
                Camera.main.transform.localRotation = originalCameraRotation;
                Camera.main.clearFlags = originalCameraClearFlags;
            }

            // Destroy the 2D flat background
            if (customBackgroundGO != null)
            {
                Destroy(customBackgroundGO);
                customBackgroundGO = null;
            }

            // Find the correct panel to restore
            GameObject panelGo = null;
            var allObjects = Object.FindObjectsOfType<GameObject>();
            foreach (var go in allObjects)
            {
                if (go != null && go.name == "Panel")
                {
                    panelGo = go;
                    break;
                }
            }

            // Re-enable and restore Panel and all CardPanels visibility
            if (panelGo != null)
            {
                var panelImage = panelGo.GetComponent<Image>();
                if (panelImage != null) panelImage.enabled = true;
            }

            // Re-enable all CardPanels that we set inactive
            foreach (var cardPanel in disabledCardPanels)
            {
                if (cardPanel != null)
                {
                    cardPanel.SetActive(true);
                    
                    var themeable = cardPanel.GetComponent<ThemeableUI>();
                    if (themeable != null)
                    {
                        themeable.enabled = true;
                        themeable.ApplyTheme();
                    }
                }
            }

            // Restore the original skybox material to restore default scene styling
            if (hasAppliedSkybox && originalSkyboxMaterial != null)
            {
                RenderSettings.skybox = originalSkyboxMaterial;
                DynamicGI.UpdateEnvironment();
                hasAppliedSkybox = false;
            }

            if (timerText != null)
                timerText.gameObject.SetActive(false);
            if (progressBarFill != null)
                progressBarFill.gameObject.SetActive(false);

            LayoutBaselineResultsCard(heartRate);
        }

        private void LayoutBaselineResultsCard(int heartRate)
        {
            Transform card = FindResultsCard();
            if (card != null)
            {
                var vlg = card.GetComponent<VerticalLayoutGroup>();
                if (vlg != null)
                    vlg.enabled = false;
                var fitter = card.GetComponent<ContentSizeFitter>();
                if (fitter != null)
                    fitter.enabled = false;
            }

            if (resultText != null)
            {
                resultText.gameObject.SetActive(true);
                resultText.text = $"Baseline heart rate ({SessionTiming.BaselineSeconds:0}s average): {heartRate} BPM";
                PlaceResultsText(resultText, card, new Vector2(0.08f, 0.58f), new Vector2(0.92f, 0.82f), 26f, TextAlignmentOptions.Center);
            }

            if (instructionText != null)
            {
                instructionText.gameObject.SetActive(true);
                instructionText.text =
                    "Baseline complete. Stay relaxed until you enter the Safe Room.\n" +
                    "During exposure, press A (right) or X (left) for Emergency / Safe Room.\n\n" +
                    UserData.AiStageRecommendation;
                PlaceResultsText(instructionText, card, new Vector2(0.08f, 0.28f), new Vector2(0.92f, 0.56f), 20f, TextAlignmentOptions.Center);
            }

            if (stageText != null)
            {
                stageText.gameObject.SetActive(true);
                PlaceResultsText(stageText, card, new Vector2(0.08f, 0.84f), new Vector2(0.92f, 0.96f), 30f, TextAlignmentOptions.Center);
            }

            if (continueButton == null)
                return;

            if (card != null)
                continueButton.transform.SetParent(card, false);

            IgnoreLayout(continueButton.gameObject);
            var rect = continueButton.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.28f, 0.10f);
            rect.anchorMax = new Vector2(0.72f, 0.10f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(0f, 56f);
            rect.localRotation = Quaternion.identity;
            rect.localScale = Vector3.one;

            var btnText = continueButton.GetComponentInChildren<TextMeshProUGUI>();
            if (btnText != null)
            {
                btnText.text = "Continue";
                btnText.enableWordWrapping = false;
                btnText.overflowMode = TextOverflowModes.Overflow;
                ClinicalUIFactory.AlignButtonLabel(btnText);
            }
            else
            {
                var legacyText = continueButton.GetComponentInChildren<Text>();
                if (legacyText != null)
                    legacyText.text = "Continue";
            }

            continueButton.transform.SetAsLastSibling();
            continueButton.gameObject.SetActive(true);
            continueButton.onClick.RemoveAllListeners();
            continueButton.onClick.AddListener(() => SceneLoader.Instance.LoadScene("SafeRoomScene"));
        }

        private Transform FindResultsCard()
        {
            foreach (var card in disabledCardPanels)
            {
                if (card != null && card.activeInHierarchy)
                    return card.transform;
            }

            var named = GameObject.Find("CardPanel");
            return named != null ? named.transform : null;
        }

        private void PlaceResultsText(TextMeshProUGUI tmp, Transform card, Vector2 anchorMin, Vector2 anchorMax, float fontSize, TextAlignmentOptions align)
        {
            if (tmp == null)
                return;

            if (card != null)
                tmp.transform.SetParent(card, false);

            IgnoreLayout(tmp.gameObject);
            var rect = tmp.rectTransform;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localRotation = Quaternion.identity;
            rect.localScale = Vector3.one;
            tmp.alignment = align;
            tmp.enableWordWrapping = true;
            tmp.overflowMode = TextOverflowModes.Overflow;
            tmp.fontSize = fontSize;
            tmp.fontStyle = FontStyles.Normal;
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

        private void EnsureSkipButton()
        {
            if (!Application.isEditor || continueButton == null || continueButton.transform.parent == null)
                return;

            if (transform.Find("SkipBaselineButton") != null)
                return;

            var skip = new GameObject("SkipBaselineButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            skip.transform.SetParent(continueButton.transform.parent, false);
            var rect = skip.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.7f, 0.02f);
            rect.anchorMax = new Vector2(0.98f, 0.10f);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            skip.GetComponent<Image>().color = new Color(1, 1, 1, 0.15f);
            var textGO = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            textGO.transform.SetParent(skip.transform, false);
            var tRect = textGO.GetComponent<RectTransform>();
            tRect.anchorMin = Vector2.zero;
            tRect.anchorMax = Vector2.one;
            tRect.offsetMin = tRect.offsetMax = Vector2.zero;
            var tmp = textGO.GetComponent<TextMeshProUGUI>();
            tmp.text = "Skip (editor)";
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = 16;
            skip.GetComponent<Button>().onClick.AddListener(() => SessionTiming.SkipRequested = true);
        }
    }
}

