using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using PhobiaReliefTherapy.Data;
using PhobiaReliefTherapy.Managers;
using PhobiaReliefTherapy.Theme;
using PhobiaReliefTherapy.UI;
using PhobiaReliefTherapy;
using PhobiaReliefTherapy.Bootstrap;

namespace PhobiaReliefTherapy.Therapy
{
    /// <summary>
    /// Post-session feedback summary (SRS §3.2.8, UC-11).
    /// </summary>
    public class FeedbackManager : MonoBehaviour
    {
        [Header("UI Elements")]
        public TextMeshProUGUI titleText;
        public TextMeshProUGUI summaryText;
        public TMP_InputField commentsInput;
        public Button saveButton;
        public Button skipButton;
        public TextMeshProUGUI statusText;

        private void Start()
        {
            VRUIInputBridge.EnsureInstanceExists();
            VRLocomotionBridge.EnsureInstanceExists();
            AutoBindMissingFields();
            Phase2RuntimeUI.EnsureFeedbackUI(this);

            VRManager.EnsureInstanceExists();
            VRManager.Instance.InitializeVR();

            PopulateSummary();
            StyleCommentsField();
            LocalSessionStore.PersistFromCurrentMetrics();

            if (saveButton != null)
            {
                saveButton.onClick.RemoveAllListeners();
                saveButton.onClick.AddListener(() => StartCoroutine(SaveAndContinue(false)));
            }

            if (skipButton != null)
            {
                skipButton.onClick.RemoveAllListeners();
                skipButton.onClick.AddListener(() => StartCoroutine(SaveAndContinue(true)));
            }

            StartCoroutine(ShowFeedbackInHeadset());
        }

        private System.Collections.IEnumerator ShowFeedbackInHeadset()
        {
            yield return null;
            yield return null;

            Transform card = titleText != null ? titleText.transform.parent : null;
            if (card == null && summaryText != null)
                card = summaryText.transform.parent;
            if (card == null)
                yield break;

            Canvas oldCanvas = card.GetComponentInParent<Canvas>();
            var worldGo = GameObject.Find("FeedbackWorldCanvas");
            if (worldGo == null)
                worldGo = new GameObject("FeedbackWorldCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));

            var worldCanvas = worldGo.GetComponent<Canvas>();
            worldCanvas.renderMode = RenderMode.WorldSpace;
            worldCanvas.sortingOrder = 120;
            var scaler = worldGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            scaler.scaleFactor = 1f;

            var worldRect = worldGo.GetComponent<RectTransform>();
            worldRect.sizeDelta = new Vector2(1000f, 820f);
            worldRect.anchorMin = worldRect.anchorMax = new Vector2(0.5f, 0.5f);
            worldRect.pivot = new Vector2(0.5f, 0.5f);

            card.SetParent(worldGo.transform, false);
            card.localScale = Vector3.one;
            card.localRotation = Quaternion.identity;
            var cardRect = card.GetComponent<RectTransform>();
            if (cardRect != null)
            {
                cardRect.anchorMin = cardRect.anchorMax = new Vector2(0.5f, 0.5f);
                cardRect.pivot = new Vector2(0.5f, 0.5f);
                cardRect.anchoredPosition = Vector2.zero;
                cardRect.sizeDelta = new Vector2(900f, 720f);
            }

            card.gameObject.SetActive(true);
            worldGo.SetActive(true);

            if (oldCanvas != null && oldCanvas.gameObject != worldGo)
                oldCanvas.enabled = false;

            PhobiaReliefTherapy.VR.VrWorldMenu.EnsureVrRaycaster(worldCanvas);

            float waited = 0f;
            while (waited < 4f)
            {
                Camera cam = PhobiaReliefTherapy.VR.VrWorldMenu.FindVrCamera();
                if (cam != null)
                {
                    worldCanvas.worldCamera = cam;
                    var dragger = worldGo.GetComponent<PhobiaReliefTherapy.VR.VRMenuDragger>();
                    if (dragger == null || !dragger.UserPlaced)
                    {
                        PhobiaReliefTherapy.VR.VrWorldMenu.PlaceInFront(worldCanvas, cam, 1.25f, ignoreSavedPose: true);
                        PhobiaReliefTherapy.VR.VrWorldMenu.AttachAndPlace(worldCanvas, cam, recenterIfLoose: false);
                    }
                    else
                    {
                        PhobiaReliefTherapy.VR.VrWorldMenu.EnsureVrRaycaster(worldCanvas);
                        dragger.RefreshCollider();
                    }
                }

                waited += Time.deltaTime;
                yield return null;
            }
        }

        private void StyleCommentsField()
        {
            if (commentsInput == null)
                return;

            var dark = new Color(0.075f, 0.082f, 0.102f, 1f);
            var themeable = commentsInput.GetComponent<ThemeableUI>();
            if (themeable == null)
                themeable = commentsInput.gameObject.AddComponent<ThemeableUI>();
            themeable.elementType = UIElementType.InputField;
            themeable.overrideTypography = true;
            themeable.ApplyTheme();

            foreach (var img in commentsInput.GetComponentsInChildren<Image>(true))
            {
                if (img == null)
                    continue;
                var gradient = img.GetComponent<UIGradient>();
                if (gradient != null)
                    Destroy(gradient);
                img.color = dark;
                img.raycastTarget = img.gameObject == commentsInput.gameObject;
            }

            var rootImage = commentsInput.GetComponent<Image>();
            if (rootImage != null)
            {
                rootImage.color = dark;
                rootImage.raycastTarget = true;
            }

            if (commentsInput.textComponent != null)
            {
                var text = commentsInput.textComponent;
                text.color = Color.white;
                text.fontSize = 20;
                text.alignment = TextAlignmentOptions.Left;
                ClinicalUIFactory.Stretch(text.rectTransform, 14f, 8f);
            }

            if (commentsInput.placeholder is TextMeshProUGUI placeholder)
            {
                placeholder.color = new Color(0.86f, 0.88f, 0.92f, 0.65f);
                placeholder.fontSize = 20;
                placeholder.fontStyle = FontStyles.Italic;
                ClinicalUIFactory.Stretch(placeholder.rectTransform, 14f, 8f);
            }

            commentsInput.caretColor = new Color32(255, 87, 34, 255);
            commentsInput.selectionColor = new Color32(255, 87, 34, 60);
        }

        private void AutoBindMissingFields()
        {
            if (titleText == null)
                titleText = AutoBindField<TextMeshProUGUI>("FeedbackTitle");
            if (summaryText == null)
                summaryText = AutoBindField<TextMeshProUGUI>("FeedbackSummaryText");
            if (commentsInput == null)
                commentsInput = AutoBindField<TMP_InputField>("FeedbackCommentsInput");
            if (saveButton == null)
                saveButton = AutoBindField<Button>("SaveFeedbackButton");
            if (skipButton == null)
                skipButton = AutoBindField<Button>("SkipFeedbackButton");
            if (statusText == null)
                statusText = AutoBindField<TextMeshProUGUI>("FeedbackStatusText");
        }

        private T AutoBindField<T>(string objectName) where T : Component
        {
            T result = AutoBindHelper.FindComponentInChildrenByName<T>(transform, objectName);
            return result != null ? result : AutoBindHelper.FindComponentByName<T>(objectName);
        }

        private void PopulateSummary()
        {
            if (titleText != null)
                titleText.text = UserData.SessionWasAborted ? "Session Ended Early" : "Session Complete";

            if (summaryText != null)
            {
                summaryText.text =
                    $"User: {UserData.Username}\n" +
                    $"Phobia: {UserData.SelectedPhobia}\n" +
                    $"Difficulty: {UserData.SelectedDifficulty} (Stage {UserData.CurrentStage})\n\n" +
                    $"Baseline HR: {UserData.BaselineHeartRate} BPM\n" +
                    $"Peak HR: {SessionMetrics.PeakHeartRate} BPM\n" +
                    $"Average HR: {SessionMetrics.AverageHeartRate:F0} BPM\n" +
                    $"Final HR: {SessionMetrics.FinalHeartRate} BPM\n" +
                    $"Duration: {SessionMetrics.TotalExposureDuration:F0}s\n\n" +
                    $"Panic Events: {SessionMetrics.PanicEventCount}\n" +
                    $"Safety Triggers: {SessionMetrics.SafetyTriggerCount}\n" +
                    $"Highest Panic Score: {SessionMetrics.HighestPanicScore:F1}\n" +
                    $"Average Panic Score: {SessionMetrics.AveragePanicScore:F1}\n" +
                    $"AI Recommendation: {SessionMetrics.LastAiRecommendation}\n\n" +
                    "Optional: type a comment, then Save. Skip still stores the session.";
            }
        }

        private IEnumerator SaveAndContinue(bool skippedRating)
        {
            if (statusText != null)
                statusText.text = "Saving session...";

            string comments = skippedRating ? "" : (commentsInput != null ? commentsInput.text.Trim() : "");
            string feedbackPayload = string.IsNullOrEmpty(comments)
                ? SessionMetrics.LastAiRecommendation
                : comments;

            var localSession = LocalSessionStore.UpdateOpenFeedback(feedbackPayload);
            LocalNotificationStore.Add(UserData.UserId, "Session saved",
                $"{UserData.SelectedPhobia} session stored. Peak HR {SessionMetrics.PeakHeartRate} BPM.");

            bool saveComplete = false;
            bool cloudSaved = false;
            yield return DatabaseManager.Instance.SaveTherapySessionFromLocal(localSession, (success) =>
            {
                cloudSaved = success;
                saveComplete = true;
            });

            while (!saveComplete)
                yield return null;

            if (statusText != null)
            {
                statusText.text = cloudSaved
                    ? "Saved to Supabase. Returning to dashboard..."
                    : "Saved locally. Cloud sync pending. Returning to dashboard...";
            }

            yield return new WaitForSeconds(0.8f);
            LocalSessionStore.ClearOpenSession();
            SceneLoader.Instance.LoadScene("DashboardScene");
        }
    }
}
