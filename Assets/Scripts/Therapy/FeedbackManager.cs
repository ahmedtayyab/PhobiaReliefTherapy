using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using PhobiaReliefTherapy.Data;
using PhobiaReliefTherapy.Managers;
using PhobiaReliefTherapy.Theme;
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

            Transform card = titleText != null ? titleText.transform.parent : null;
            if (card == null && summaryText != null)
                card = summaryText.transform.parent;
            if (card == null)
                yield break;

            Canvas oldCanvas = card.GetComponentInParent<Canvas>();

            var worldGo = new GameObject("FeedbackWorldCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var worldCanvas = worldGo.GetComponent<Canvas>();
            worldCanvas.renderMode = RenderMode.WorldSpace;
            worldCanvas.sortingOrder = 80;
            var scaler = worldGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            scaler.scaleFactor = 1f;
            var worldRect = worldGo.GetComponent<RectTransform>();
            worldRect.sizeDelta = new Vector2(1000f, 820f);

            card.SetParent(worldGo.transform, false);
            var cardRect = card.GetComponent<RectTransform>();
            if (cardRect != null)
            {
                cardRect.anchorMin = cardRect.anchorMax = new Vector2(0.5f, 0.5f);
                cardRect.anchoredPosition = Vector2.zero;
                if (cardRect.sizeDelta.x < 200f)
                    cardRect.sizeDelta = new Vector2(900f, 720f);
            }

            if (oldCanvas != null && oldCanvas != worldCanvas)
                oldCanvas.enabled = false;

            float waited = 0f;
            while (waited < 2.5f)
            {
                Camera cam = PhobiaReliefTherapy.VR.VrWorldMenu.FindVrCamera();
                if (cam != null)
                {
                    worldCanvas.worldCamera = cam;
                    PhobiaReliefTherapy.VR.VrWorldMenu.AttachAndPlace(worldCanvas, cam, recenterIfLoose: true);
                    PhobiaReliefTherapy.VR.VrWorldMenu.PlaceInFront(worldCanvas, cam, 1.3f);
                }

                waited += Time.deltaTime;
                yield return null;
            }
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
            SceneLoader.Instance.LoadScene("DashboardScene");
        }
    }
}
