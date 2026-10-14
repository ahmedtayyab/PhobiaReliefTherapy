using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using PhobiaReliefTherapy.Data;
using PhobiaReliefTherapy.Managers;
using PhobiaReliefTherapy.Theme;
using PhobiaReliefTherapy;

namespace PhobiaReliefTherapy.Therapy
{
    /// <summary>
    /// Manages the Safe Room environment and transitioning to the correct exposure scene.
    /// </summary>
    public class SafeRoomManager : MonoBehaviour
    {
        [Header("UI Elements")]
        public TextMeshProUGUI welcomeText;
        public TextMeshProUGUI infoText;
        public TextMeshProUGUI statusText;
        public Button startExposureButton;
        public Button backToDashboardButton;

        private TextMeshProUGUI recommendationText;

        private void Start()
        {
            VRUIInputBridge.EnsureInstanceExists();
            VRLocomotionBridge.EnsureInstanceExists();
            AutoBindMissingFields();
            InitializeSafeRoomImage();

            VRManager.EnsureInstanceExists();
            SensorManager.EnsureInstanceExists();

            if (welcomeText != null)
                welcomeText.text = "Welcome";

            if (infoText != null)
            {
                infoText.text =
                    $"Selected phobia: {UserData.SelectedPhobia}\n" +
                    $"Baseline HR: {UserData.BaselineHeartRate} BPM\n" +
                    $"AI exposure stage: {UserData.CurrentStage}";
            }

            EnsureRecommendationText();
            if (recommendationText != null)
            {
                recommendationText.text = string.IsNullOrEmpty(UserData.AiStageRecommendation)
                    ? "Press Continue when you are ready. During exposure, A or X returns you here."
                    : UserData.AiStageRecommendation + "\nDuring exposure, press A (right) or X (left) for Emergency / Safe Room.";
            }

            VRManager.Instance.InitializeVR();
            SensorManager.Instance.HoldBaselineHeartRate();
            AdjustSafeRoomLayout();

            if (UserData.SessionWasAborted)
            {
                if (infoText != null)
                {
                    infoText.text =
                        $"Selected phobia: {UserData.SelectedPhobia}\n" +
                        $"Baseline HR: {UserData.BaselineHeartRate} BPM\n" +
                        "Session paused in the Safe Room after a safety stop.";
                }

                if (recommendationText != null)
                    recommendationText.text = "You are safe. Review feedback when you are ready, or stay here.";

                if (startExposureButton != null)
                {
                    var label = startExposureButton.GetComponentInChildren<TextMeshProUGUI>();
                    if (label != null)
                        label.text = "View Feedback";
                    startExposureButton.onClick.RemoveAllListeners();
                    startExposureButton.onClick.AddListener(() => SceneLoader.Instance.LoadScene("FeedbackScene"));
                }
            }
            else if (startExposureButton != null)
            {
                startExposureButton.onClick.RemoveAllListeners();
                startExposureButton.onClick.AddListener(StartExposure);

                var label = startExposureButton.GetComponentInChildren<TextMeshProUGUI>();
                if (label != null)
                    label.text = "Start Exposure";
            }

            if (backToDashboardButton != null)
            {
                backToDashboardButton.onClick.RemoveAllListeners();
                backToDashboardButton.onClick.AddListener(() =>
                {
                    if (UserData.SessionWasAborted)
                        LocalSessionStore.PersistFromCurrentMetrics();
                    SceneLoader.Instance.LoadScene("DashboardScene");
                });
            }
        }

        private void EnsureRecommendationText()
        {
            if (infoText == null)
                return;

            recommendationText = AutoBindHelper.FindComponentInChildrenByName<TextMeshProUGUI>(
                infoText.transform.parent, "RecommendationText");

            if (recommendationText != null)
                return;

            var go = new GameObject("RecommendationText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            go.transform.SetParent(infoText.transform.parent, false);

            int buttonIndex = startExposureButton != null
                ? startExposureButton.transform.GetSiblingIndex()
                : infoText.transform.parent.childCount;
            go.transform.SetSiblingIndex(buttonIndex);

            recommendationText = go.GetComponent<TextMeshProUGUI>();
            recommendationText.fontSize = 22f;
            recommendationText.alignment = TextAlignmentOptions.Center;
            recommendationText.enableWordWrapping = true;
            recommendationText.color = new Color(0.92f, 0.92f, 0.95f, 0.95f);

            var theme = go.AddComponent<ThemeableUI>();
            theme.elementType = UIElementType.BodyText;
            theme.ApplyTheme();
        }

        private void InitializeSafeRoomImage()
        {
            GameObject existingImg = GameObject.Find("SafeRoomImage");
            if (existingImg != null)
                return;

            GameObject emptyCard = null;
            var canvases = Object.FindObjectsOfType<Canvas>();
            foreach (var canvas in canvases)
            {
                var panelTransform = canvas.transform.Find("Panel");
                if (panelTransform != null)
                {
                    foreach (Transform child in panelTransform)
                    {
                        if (child.name.Contains("CardPanel") && child.childCount == 0)
                        {
                            emptyCard = child.gameObject;
                            break;
                        }
                    }
                }
                if (emptyCard != null) break;
            }

            if (emptyCard == null)
            {
                var cards = Object.FindObjectsOfType<VerticalLayoutGroup>();
                foreach (var card in cards)
                {
                    if (card.gameObject.name.Contains("CardPanel") && card.transform.childCount == 0)
                    {
                        emptyCard = card.gameObject;
                        break;
                    }
                }
            }

            if (emptyCard != null)
            {
                var imageGO = new GameObject("SafeRoomImage", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                imageGO.transform.SetParent(emptyCard.transform, false);

                var image = imageGO.GetComponent<Image>();
                var sprite = Resources.Load<Sprite>("safe_room_preview");
                if (sprite != null)
                {
                    image.sprite = sprite;
                    image.preserveAspect = true;
                }

                var layout = imageGO.AddComponent<LayoutElement>();
                layout.preferredWidth = 600f;
                layout.preferredHeight = 400f;
                layout.minWidth = 600f;
                layout.minHeight = 400f;
            }
        }

        private void AutoBindMissingFields()
        {
            if (welcomeText == null)
                welcomeText = AutoBindField<TextMeshProUGUI>("WelcomeText");
            if (infoText == null)
                infoText = AutoBindField<TextMeshProUGUI>("InfoText");
            if (statusText == null)
                statusText = AutoBindField<TextMeshProUGUI>("StatusText");
            if (startExposureButton == null)
                startExposureButton = AutoBindField<Button>("StartExposureButton");
            if (backToDashboardButton == null)
                backToDashboardButton = AutoBindField<Button>("BackToDashboardButton");
        }

        private T AutoBindField<T>(string objectName) where T : Component
        {
            T result = AutoBindHelper.FindComponentInChildrenByName<T>(transform, objectName);
            return result != null ? result : AutoBindHelper.FindComponentByName<T>(objectName);
        }

        private void AdjustSafeRoomLayout()
        {
            if (infoText == null)
                return;

            Transform cardPanel = infoText.transform.parent;
            if (cardPanel == null)
                return;

            var layoutGroup = cardPanel.GetComponent<VerticalLayoutGroup>();
            if (layoutGroup != null)
            {
                layoutGroup.childAlignment = TextAnchor.UpperCenter;
                layoutGroup.spacing = 20f;
                layoutGroup.padding = new RectOffset(40, 40, 48, 48);
                layoutGroup.childControlWidth = true;
                layoutGroup.childControlHeight = true;
                layoutGroup.childForceExpandWidth = true;
                layoutGroup.childForceExpandHeight = false;
            }

            ApplyTextLayout(welcomeText, 56f, 36f);
            ApplyTextLayout(infoText, 120f, 26f);
            ApplyTextLayout(recommendationText, 72f, 22f);

            if (infoText != null)
            {
                infoText.alignment = TextAlignmentOptions.Center;
                infoText.lineSpacing = 6f;
            }

            if (recommendationText != null)
                recommendationText.alignment = TextAlignmentOptions.Center;

            if (startExposureButton != null)
            {
                startExposureButton.transform.SetAsLastSibling();
                ApplyButtonLayout(startExposureButton, 56f);
            }

            var cardRect = cardPanel.GetComponent<RectTransform>();
            if (cardRect != null)
            {
                cardRect.sizeDelta = new Vector2(560f, 760f);
                LayoutRebuilder.ForceRebuildLayoutImmediate(cardRect);
            }
        }

        private static void ApplyTextLayout(TextMeshProUGUI text, float preferredHeight, float fontSize)
        {
            if (text == null)
                return;

            text.fontSize = fontSize;
            text.enableWordWrapping = true;

            var rect = text.rectTransform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = new Vector2(0f, preferredHeight);

            var layout = text.GetComponent<LayoutElement>();
            if (layout == null)
                layout = text.gameObject.AddComponent<LayoutElement>();
            layout.preferredHeight = preferredHeight;
            layout.minHeight = preferredHeight * 0.75f;
            layout.flexibleHeight = 0f;
        }

        private static void ApplyButtonLayout(Button button, float height)
        {
            var rect = button.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(0f, height);

            var layout = button.GetComponent<LayoutElement>();
            if (layout == null)
                layout = button.gameObject.AddComponent<LayoutElement>();
            layout.preferredHeight = height;
            layout.minHeight = height;
            layout.flexibleHeight = 0f;
        }

        private void StartExposure()
        {
            SensorManager.EnsureInstanceExists();
            if (!SensorManager.Instance.IsProviderReady || SensorManager.Instance.UseMockSensor)
                SensorManager.Instance.ReconnectLiveSensor();

            if (!SessionTiming.LaptopPlaytest && !SensorManager.Instance.IsSensorConnected)
            {
                if (recommendationText != null)
                    recommendationText.text = "Connect Polar H10 and keep python tools\\polar_hr_bridge.py running before starting exposure.";
                return;
            }

            string scene = PhobiaCatalog.ExposureSceneName(UserData.SelectedPhobia);
            if (string.IsNullOrEmpty(scene))
            {
                if (recommendationText != null)
                    recommendationText.text = "No phobia is selected. Go back and choose Height, Darkness, or Crowd.";
                return;
            }

            if (startExposureButton != null)
                startExposureButton.interactable = false;

            StartCoroutine(CountdownAndEnter(scene));
        }

        private IEnumerator CountdownAndEnter(string scene)
        {
            float remaining = SessionTiming.SafeRoomCountdownSeconds;
            while (remaining > 0f)
            {
                if (SessionTiming.ConsumeSkip())
                    break;

                if (recommendationText != null)
                    recommendationText.text = $"Entering exposure in {Mathf.CeilToInt(remaining)}... Stay in the Safe Room if you are not ready.";
                yield return new WaitForSeconds(1f);
                remaining -= 1f;
            }

            UserData.SelectedPhobia = PhobiaCatalog.Normalize(UserData.SelectedPhobia);
            Debug.Log($"Starting Exposure for: {UserData.SelectedPhobia} -> {scene}");
            SceneLoader.Instance.LoadScene(scene);
        }
    }
}
