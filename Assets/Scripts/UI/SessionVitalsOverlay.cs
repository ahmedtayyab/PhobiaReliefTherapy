using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR;
using TMPro;
using PhobiaReliefTherapy;
using PhobiaReliefTherapy.Data;
using PhobiaReliefTherapy.Managers;
using PhobiaReliefTherapy.Theme;
using PhobiaReliefTherapy.Therapy;

namespace PhobiaReliefTherapy.UI
{
    /// <summary>
    /// Small live graphs on the session edges (UC-08 / UC-10 / UC-11).
    /// Toggle with VR secondary button (B / Y) or G in the Editor. Layout never rebuilds.
    /// </summary>
    public class SessionVitalsOverlay : MonoBehaviour
    {
        private const float SampleInterval = 0.25f;
        private const float HudDistance = 1.2f;
        private const float HudRight = 0.22f;
        private const float HudDown = 0.16f;
        private const float HudScale = 0.00105f;

        private Canvas canvas;
        private CanvasGroup graphsGroup;
        private RectTransform graphsRoot;
        private Button toggleButton;
        private TextMeshProUGUI toggleLabel;
        private TextMeshProUGUI hintLabel;

        private TextMeshProUGUI hrValue;
        private TextMeshProUGUI moveValue;
        private TextMeshProUGUI stressValue;
        private UiAiMark aiMark;
        private UiSparkline hrGraph;
        private UiSparkline moveGraph;
        private UiSparkline stressGraph;
        private Color aiAccent;
        private float aiPulseTimer;

        private bool graphsVisible = true;
        private bool followHead = true;
        private bool parked;
        private bool secondaryWasDown;
        private float sampleTimer;
        private float consecutiveStressSeconds;
        private string lastClassification = "Calm Stress";
        private Color calmColor;
        private Color warnColor;
        private Color dangerColor;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (Object.FindObjectOfType<SessionVitalsOverlay>() != null)
                return;

            var go = new GameObject("SessionVitalsOverlay");
            go.AddComponent<SessionVitalsOverlay>();
            DontDestroyOnLoad(go);
        }

        private void Awake()
        {
            BuildUi();
            var dragger = canvas != null ? canvas.GetComponent<PhobiaReliefTherapy.VR.VRMenuDragger>() : null;
            if (canvas != null && dragger == null)
                dragger = canvas.gameObject.AddComponent<PhobiaReliefTherapy.VR.VRMenuDragger>();
            if (dragger != null)
                dragger.IsVitalsHud = true;
            PhobiaReliefTherapy.VR.VRMenuDragger.GrabStarted += OnHudGrabbed;
            SceneManager.sceneLoaded += OnSceneLoaded;
            RefreshSceneVisibility();
        }

        private void OnDestroy()
        {
            PhobiaReliefTherapy.VR.VRMenuDragger.GrabStarted -= OnHudGrabbed;
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnHudGrabbed(PhobiaReliefTherapy.VR.VRMenuDragger dragger)
        {
            if (dragger != null && dragger.IsVitalsHud)
            {
                followHead = false;
                parked = true;
            }
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            RefreshSceneVisibility();
        }

        private void Update()
        {
            if (canvas == null || !canvas.gameObject.activeSelf)
                return;

            HandleToggleInput();
            TickAiThink();

            if (IsReviewScene())
            {
                RefreshGraphs();
                return;
            }

            sampleTimer += Time.deltaTime;
            if (sampleTimer < SampleInterval)
                return;

            sampleTimer = 0f;
            SampleLive();
            RefreshGraphs();
        }

        private void LateUpdate()
        {
            if (canvas == null || !canvas.gameObject.activeSelf)
                return;

            Camera cam = Camera.main;
            if (cam == null || !VrAvailability.IsHeadsetPresent() || parked || !followHead)
                return;

            if (canvas.renderMode != RenderMode.WorldSpace)
            {
                canvas.renderMode = RenderMode.WorldSpace;
                canvas.worldCamera = cam;
                var rect = canvas.GetComponent<RectTransform>();
                rect.sizeDelta = new Vector2(400f, 560f);
                canvas.transform.localScale = Vector3.one * HudScale;
                if (graphsRoot != null)
                    ClinicalUIFactory.Stretch(graphsRoot, 8f, 48f);
                if (toggleButton != null)
                    ClinicalUIFactory.Anchor(toggleButton.GetComponent<RectTransform>(),
                        new Vector2(0.08f, 0.015f), new Vector2(0.92f, 0.015f),
                        new Vector2(0f, 36f), Vector2.zero);
                if (hintLabel != null)
                    hintLabel.gameObject.SetActive(false);
                var hudDrag = canvas.GetComponent<PhobiaReliefTherapy.VR.VRMenuDragger>();
                if (hudDrag != null)
                    hudDrag.RefreshCollider();
            }

            if (!PhobiaReliefTherapy.VR.VrMenuPose.TryApplyHud(canvas.transform, cam))
            {
                Vector3 pos = cam.transform.position
                              + cam.transform.forward * HudDistance
                              + cam.transform.right * HudRight
                              + cam.transform.up * -HudDown;
                canvas.transform.position = pos;
                canvas.transform.rotation = Quaternion.LookRotation(pos - cam.transform.position, cam.transform.up);
            }

            parked = true;
            followHead = false;
        }

        private void HandleToggleInput()
        {
            bool requested = Input.GetKeyDown(KeyCode.G);
            bool secondary = IsSecondaryButtonDown();
            if (secondary && !secondaryWasDown)
                requested = true;
            secondaryWasDown = secondary;

            if (requested)
                SetGraphsVisible(!graphsVisible);
        }

        private static bool IsSecondaryButtonDown()
        {
            return IsSecondary(XRNode.RightHand) || IsSecondary(XRNode.LeftHand);
        }

        private static bool IsSecondary(XRNode node)
        {
            InputDevice device = InputDevices.GetDeviceAtXRNode(node);
            if (!device.isValid)
                return false;

            return device.TryGetFeatureValue(CommonUsages.secondaryButton, out bool pressed) && pressed;
        }

        private void SampleLive()
        {
            SensorManager.EnsureInstanceExists();
            HeadMovementTracker.EnsureInstanceExists();

            int heartRate = SensorManager.Instance != null ? SensorManager.Instance.CurrentHeartRate : 0;
            float movement = HeadMovementTracker.Instance != null ? HeadMovementTracker.Instance.AgitationScore : 0f;
            float jerk = HeadMovementTracker.Instance != null ? HeadMovementTracker.Instance.JerkScore : 0f;
            float combined = movement;

            float deltaHr = heartRate - Mathf.Max(1, UserData.BaselineHeartRate);
            bool stressed = deltaHr > 10f ||
                            (SessionMetrics.BaselineHeadMovementScore > 0.01f &&
                             combined > SessionMetrics.BaselineHeadMovementScore * 1.5f);
            consecutiveStressSeconds = stressed ? consecutiveStressSeconds + SampleInterval : 0f;

            var evaluation = PanicDetectionService.Evaluate(heartRate, combined, consecutiveStressSeconds);
            lastClassification = evaluation.Classification;
            aiPulseTimer = 0.35f;
            SessionVitalsHistory.Record(heartRate, movement, jerk, evaluation.PanicScore);
        }

        private void RefreshGraphs()
        {
            if (hrGraph == null)
                return;

            int baseline = UserData.BaselineHeartRate > 0 ? UserData.BaselineHeartRate : 80;
            hrGraph.MinValue = Mathf.Max(40f, baseline - 18f);
            hrGraph.MaxValue = Mathf.Min(180f, Mathf.Max(baseline + 36f, 110f));
            hrGraph.SetSeries(SessionVitalsHistory.HeartRates);
            moveGraph.SetSeries(SessionVitalsHistory.MovementScores, SessionVitalsHistory.JerkScores);
            stressGraph.SetSeries(SessionVitalsHistory.PanicScores);

            int hr = Mathf.RoundToInt(SessionVitalsHistory.LastHeartRate);
            hrValue.text = hr > 0 ? $"{hr} BPM" : "— BPM";
            moveValue.text = SessionVitalsHistory.LastJerk >= 20f
                ? $"Move {SessionVitalsHistory.LastMovement:F0}  ·  jerk spike"
                : $"Move {SessionVitalsHistory.LastMovement:F0}  ·  jerk {SessionVitalsHistory.LastJerk:F0}";
            stressValue.text = $"{SessionVitalsHistory.LastPanic:F1}  ·  {lastClassification}";

            Color stressColor = SessionVitalsHistory.LastPanic >= 18f
                ? dangerColor
                : SessionVitalsHistory.LastPanic >= 10f
                    ? warnColor
                    : calmColor;
            stressGraph.LineColor = stressColor;
            stressGraph.FillColor = new Color(stressColor.r, stressColor.g, stressColor.b, 0.22f);
        }

        private void SetGraphsVisible(bool visible)
        {
            graphsVisible = visible;
            if (graphsGroup != null)
                graphsGroup.alpha = visible ? 1f : 0f;
            if (toggleLabel != null)
                toggleLabel.text = visible ? "Hide graphs" : "Show graphs";
        }

        private void RefreshSceneVisibility()
        {
            bool show = ShouldShow();
            if (canvas != null)
            {
                var dragger = canvas.GetComponent<PhobiaReliefTherapy.VR.VRMenuDragger>();
                if (dragger != null)
                    dragger.ReleaseGrab();
                if (!show)
                    PhobiaReliefTherapy.VR.VRMenuDragger.ReleaseAllGrabs();
                canvas.gameObject.SetActive(show);
            }
            if (!show)
                return;

            parked = false;
            followHead = true;
            SetGraphsVisible(true);

            if (IsReviewScene())
            {
                float panic = SessionVitalsHistory.LastPanic;
                lastClassification = panic >= 18f ? "Severe Panic" : panic >= 10f ? "Panic Detected" : "Calm Stress";
            }
        }

        private static bool ShouldShow()
        {
            string scene = SceneManager.GetActiveScene().name;
            return scene == "BaselineScene"
                || scene == "SafeRoomScene"
                || scene == "HeightScene"
                || scene == "DarknessScene"
                || scene == "CrowdScene"
                || scene == "FeedbackScene";
        }

        private static bool IsReviewScene()
        {
            return SceneManager.GetActiveScene().name == "FeedbackScene";
        }

        private void BuildUi()
        {
            ThemePreset theme = Resources.Load<ThemePreset>("MedicalTheme");
            Color accent = theme != null ? theme.primaryMedicalBlue : new Color32(255, 87, 34, 255);
            calmColor = theme != null ? theme.successGreen : new Color32(56, 161, 105, 255);
            warnColor = accent;
            dangerColor = theme != null ? theme.errorRed : new Color32(229, 62, 62, 255);
            aiAccent = accent;

            var canvasGo = new GameObject("VitalsOverlayCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);
            canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 40;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            graphsRoot = new GameObject("Graphs", typeof(RectTransform)).GetComponent<RectTransform>();
            graphsRoot.SetParent(canvas.transform, false);
            ClinicalUIFactory.Anchor(graphsRoot, new Vector2(0.62f, 0.18f), new Vector2(0.88f, 0.82f));
            graphsGroup = graphsRoot.gameObject.AddComponent<CanvasGroup>();
            graphsGroup.blocksRaycasts = false;
            graphsGroup.interactable = false;

            CreateAiThinkBadge(graphsRoot, accent);

            hrGraph = CreateCard(graphsRoot, "HeartRateCard",
                new Vector2(0.04f, 0.68f), new Vector2(0.96f, 0.98f),
                "Heart rate", "Polar BPM", accent, 55f, 120f, false, null, out hrValue);

            stressGraph = CreateCard(graphsRoot, "StressCard",
                new Vector2(0.04f, 0.36f), new Vector2(0.96f, 0.66f),
                "Stress score", "ΔHR×0.5 + HM×0.3 + TS×0.2",
                calmColor, 0f, 30f, false, new[] { 10f, 18f }, out stressValue);

            moveGraph = CreateCard(graphsRoot, "MovementCard",
                new Vector2(0.04f, 0.04f), new Vector2(0.96f, 0.34f),
                "Head movement", "Rotation + jerk",
                theme != null ? theme.textLight : new Color(0.86f, 0.88f, 0.92f),
                0f, 80f, true, null, out moveValue);
            moveGraph.SecondaryColor = accent;

            toggleButton = ClinicalUIFactory.CreateButton("ToggleGraphsButton", canvas.transform, "Hide graphs", false);
            ClinicalUIFactory.Anchor(toggleButton.GetComponent<RectTransform>(),
                new Vector2(0.62f, 0.08f), new Vector2(0.88f, 0.08f),
                new Vector2(0f, 40f), Vector2.zero);
            toggleLabel = toggleButton.GetComponentInChildren<TextMeshProUGUI>();
            toggleButton.onClick.AddListener(() => SetGraphsVisible(!graphsVisible));

            hintLabel = ClinicalUIFactory.CreateText("GraphHint", canvas.transform,
                "A / X  ·  Safe Room     B / Y  ·  graphs", 13, TextAlignmentOptions.Center, UIElementType.LabelText);
            ClinicalUIFactory.Anchor(hintLabel.rectTransform,
                new Vector2(0.62f, 0.02f), new Vector2(0.88f, 0.06f));
            hintLabel.raycastTarget = false;
        }

        private void CreateAiThinkBadge(Transform parent, Color accent)
        {
            var badge = ClinicalUIFactory.CreateCard(parent, new Vector2(64f, 64f), "AiThinkBadge");
            ClinicalUIFactory.Anchor(badge.GetComponent<RectTransform>(),
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(64f, 64f), new Vector2(0f, 36f));
            var bg = badge.GetComponent<Image>();
            bg.raycastTarget = false;
            Color c = bg.color;
            bg.color = new Color(c.r, c.g, c.b, 0.9f);

            var markGo = new GameObject("AiMark", typeof(RectTransform), typeof(CanvasRenderer), typeof(UiAiMark));
            markGo.transform.SetParent(badge.transform, false);
            ClinicalUIFactory.Stretch(markGo.GetComponent<RectTransform>(), 8f, 8f);
            aiMark = markGo.GetComponent<UiAiMark>();
            aiMark.raycastTarget = false;
            aiMark.Accent = accent;
            aiMark.color = Color.white;
        }

        private void TickAiThink()
        {
            if (aiPulseTimer > 0f)
                aiPulseTimer -= Time.deltaTime;
            if (aiMark == null)
                return;

            aiMark.ThinkingFast = aiPulseTimer > 0f;
            aiMark.Accent = aiAccent;
            aiMark.Tick();
        }

        private UiSparkline CreateCard(Transform parent, string name, Vector2 min, Vector2 max,
            string title, string subtitle, Color lineColor, float minValue, float maxValue,
            bool autoScale, float[] thresholds, out TextMeshProUGUI valueLabel)
        {
            var card = ClinicalUIFactory.CreateCard(parent, Vector2.zero, name);
            var rect = card.GetComponent<RectTransform>();
            ClinicalUIFactory.Anchor(rect, min, max);
            var image = card.GetComponent<Image>();
            image.raycastTarget = false;
            Color c = image.color;
            image.color = new Color(c.r, c.g, c.b, 0.78f);

            var titleText = ClinicalUIFactory.CreateText(name + "Title", card.transform, title, 15,
                TextAlignmentOptions.TopLeft, UIElementType.LabelText);
            ClinicalUIFactory.Anchor(titleText.rectTransform, new Vector2(0.07f, 0.78f), new Vector2(0.93f, 0.96f));
            titleText.raycastTarget = false;

            valueLabel = ClinicalUIFactory.CreateText(name + "Value", card.transform, "—", 20,
                TextAlignmentOptions.TopLeft, UIElementType.SubheadingText);
            ClinicalUIFactory.Anchor(valueLabel.rectTransform, new Vector2(0.07f, 0.58f), new Vector2(0.93f, 0.80f));
            valueLabel.raycastTarget = false;

            var sub = ClinicalUIFactory.CreateText(name + "Sub", card.transform, subtitle, 12,
                TextAlignmentOptions.TopLeft, UIElementType.LabelText);
            ClinicalUIFactory.Anchor(sub.rectTransform, new Vector2(0.07f, 0.46f), new Vector2(0.93f, 0.60f));
            sub.raycastTarget = false;

            var graphGo = new GameObject(name + "Graph", typeof(RectTransform), typeof(CanvasRenderer), typeof(UiSparkline));
            graphGo.transform.SetParent(card.transform, false);
            var graphRect = graphGo.GetComponent<RectTransform>();
            ClinicalUIFactory.Anchor(graphRect, new Vector2(0.07f, 0.08f), new Vector2(0.93f, 0.46f));
            var graph = graphGo.GetComponent<UiSparkline>();
            graph.raycastTarget = false;
            graph.LineColor = lineColor;
            graph.FillColor = new Color(lineColor.r, lineColor.g, lineColor.b, 0.22f);
            graph.MinValue = minValue;
            graph.MaxValue = maxValue;
            graph.AutoScale = autoScale;
            graph.Thresholds = thresholds;
            return graph;
        }
    }
}
