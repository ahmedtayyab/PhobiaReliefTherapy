using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PhobiaReliefTherapy.Data;
using PhobiaReliefTherapy.Managers;
using PhobiaReliefTherapy.Theme;

namespace PhobiaReliefTherapy.Therapy
{
    /// <summary>
    /// Shared Phase 2 helpers for exposure scenes — emergency stop, session completion routing.
    /// </summary>
    public static class TherapySessionHelper
    {
        public static void BeginExposureSession(System.Action<PanicDetectionService.PanicEvaluation> onSafetyTriggered = null)
        {
            ExposureSessionMonitor.EnsureInstanceExists();
            ExposureSessionMonitor.Instance.StartMonitoring(onSafetyTriggered);
        }

        public static void CompleteExposureSession(bool aborted)
        {
            ExposureSessionMonitor.EnsureInstanceExists();
            if (ExposureSessionMonitor.Instance != null)
                ExposureSessionMonitor.Instance.StopMonitoring(aborted);

            UserData.SessionWasAborted = aborted;
            SceneLoader.Instance.LoadScene("FeedbackScene");
        }

        public static int GetStageCountForDifficulty()
        {
            switch (UserData.SelectedDifficulty)
            {
                case "Medium":
                    return 2;
                case "High":
                    return 3;
                default:
                    return 1;
            }
        }

        public static float GetSkyboxExposureForDifficulty()
        {
            switch (UserData.SelectedDifficulty)
            {
                case "Medium":
                    return 1.15f;
                case "High":
                    return 1.35f;
                default:
                    return 1.0f;
            }
        }

        public static void ApplyDifficultyToSkyboxMaterial(Material skyboxMat)
        {
            if (skyboxMat == null)
                return;

            skyboxMat.SetFloat("_Exposure", GetSkyboxExposureForDifficulty());
        }

        public static Button CreateEmergencyStopButton(Transform panelParent)
        {
            if (panelParent == null)
                return null;

            var existing = panelParent.Find("EmergencyStopButton");
            if (existing != null)
                return existing.GetComponent<Button>();

            var buttonGO = new GameObject("EmergencyStopButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            buttonGO.transform.SetParent(panelParent, false);

            var rect = buttonGO.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.12f);
            rect.anchorMax = new Vector2(0.5f, 0.12f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(260f, 52f);
            rect.anchoredPosition = Vector2.zero;

            var theme = buttonGO.AddComponent<ThemeableUI>();
            theme.elementType = UIElementType.SecondaryButton;

            var textGO = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            textGO.transform.SetParent(buttonGO.transform, false);
            var textRect = textGO.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            var tmp = textGO.GetComponent<TextMeshProUGUI>();
            tmp.text = "Emergency Stop";
            tmp.alignment = TextAlignmentOptions.Center;

            var textTheme = textGO.AddComponent<ThemeableUI>();
            textTheme.elementType = UIElementType.ButtonText;
            theme.ApplyTheme();
            textTheme.ApplyTheme();

            return buttonGO.GetComponent<Button>();
        }

        public static void WireEmergencyStop(Button emergencyButton, System.Action onTriggered)
        {
            if (emergencyButton == null)
                return;

            emergencyButton.onClick.RemoveAllListeners();
            emergencyButton.onClick.AddListener(() =>
            {
                SessionMetrics.SafetyTriggerCount++;
                onTriggered?.Invoke();
            });
        }
    }
}
