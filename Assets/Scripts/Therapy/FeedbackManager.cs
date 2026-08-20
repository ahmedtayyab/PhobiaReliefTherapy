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

            if (saveButton != null)
            {
                saveButton.onClick.RemoveAllListeners();
                saveButton.onClick.AddListener(() => StartCoroutine(SaveAndContinue()));
            }

            if (skipButton != null)
            {
                skipButton.onClick.RemoveAllListeners();
                skipButton.onClick.AddListener(() => SceneLoader.Instance.LoadScene("DashboardScene"));
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
                    $"AI Recommendation: {SessionMetrics.LastAiRecommendation}";
            }
        }

        private IEnumerator SaveAndContinue()
        {
            if (statusText != null)
                statusText.text = "Saving session...";

            string comments = commentsInput != null ? commentsInput.text.Trim() : "";
            string feedbackPayload = string.IsNullOrEmpty(comments)
                ? SessionMetrics.LastAiRecommendation
                : comments;

            var localSession = new LocalSessionStore.StoredSession
            {
                UserId = UserData.UserId,
                Phobia = UserData.SelectedPhobia,
                Difficulty = UserData.SelectedDifficulty,
                Stage = UserData.CurrentStage,
                BaselineHeartRate = UserData.BaselineHeartRate,
                FinalHeartRate = SessionMetrics.FinalHeartRate,
                PeakHeartRate = SessionMetrics.PeakHeartRate,
                AverageHeartRate = SessionMetrics.AverageHeartRate,
                ExposureTime = SessionMetrics.TotalExposureDuration,
                PanicEventCount = SessionMetrics.PanicEventCount,
                SafetyTriggerCount = SessionMetrics.SafetyTriggerCount,
                HighestPanicScore = SessionMetrics.HighestPanicScore,
                AveragePanicScore = SessionMetrics.AveragePanicScore,
                Feedback = feedbackPayload,
                DateCompleted = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            };

            LocalSessionStore.SaveSession(localSession);

            bool saveComplete = false;
            yield return DatabaseManager.Instance.SaveTherapySession(
                UserData.UserId,
                UserData.SelectedPhobia,
                UserData.SelectedDifficulty,
                UserData.CurrentStage,
                UserData.BaselineHeartRate,
                SessionMetrics.FinalHeartRate,
                SessionMetrics.TotalExposureDuration,
                feedbackPayload,
                (success) => { saveComplete = true; });

            while (!saveComplete)
                yield return null;

            if (statusText != null)
                statusText.text = "Saved. Returning to dashboard...";

            yield return new WaitForSeconds(0.8f);
            SceneLoader.Instance.LoadScene("DashboardScene");
        }
    }
}
