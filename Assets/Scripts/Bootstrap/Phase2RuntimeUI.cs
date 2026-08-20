using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using PhobiaReliefTherapy.Theme;
using PhobiaReliefTherapy.Therapy;
using PhobiaReliefTherapy.Admin;

namespace PhobiaReliefTherapy.Bootstrap
{
    /// <summary>
    /// Builds Phase 2 UI at runtime when editor scene builders have not been run.
    /// Matches existing medical card layout and ThemeableUI styling.
    /// </summary>
    public static class Phase2RuntimeUI
    {
        public static Canvas EnsureOverlayCanvas()
        {
            var existing = Object.FindObjectOfType<Canvas>();
            if (existing != null && existing.renderMode == RenderMode.ScreenSpaceOverlay)
                return existing;

            if (Object.FindObjectOfType<EventSystem>() == null)
                new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

            var canvasGO = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            return canvas;
        }

        public static void EnsureFeedbackUI(FeedbackManager manager)
        {
            if (manager.titleText != null && manager.summaryText != null)
                return;

            var canvas = EnsureOverlayCanvas();
            var card = CreateCard(canvas.transform, new Vector2(900, 720));

            manager.titleText = CreateText("FeedbackTitle", card.transform, "Session Feedback", 42, TextAlignmentOptions.Center);
            manager.summaryText = CreateText("FeedbackSummaryText", card.transform, "", 20, TextAlignmentOptions.TopLeft);
            manager.commentsInput = CreateInput("FeedbackCommentsInput", card.transform, "Optional comments...");
            manager.saveButton = CreateButton("SaveFeedbackButton", card.transform, "Save & Continue");
            manager.skipButton = CreateTextButton("SkipFeedbackButton", card.transform, "Skip");
            manager.statusText = CreateText("FeedbackStatusText", card.transform, "", 18, TextAlignmentOptions.Center);

            LayoutFeedback(
                manager.titleText.GetComponent<RectTransform>(),
                manager.summaryText.GetComponent<RectTransform>(),
                manager.commentsInput.GetComponent<RectTransform>(),
                manager.saveButton.GetComponent<RectTransform>(),
                manager.skipButton.GetComponent<RectTransform>(),
                manager.statusText.GetComponent<RectTransform>());
        }

        public static void EnsureDashboardHistoryUI(DashboardSessionHistory history)
        {
            if (history.historyText != null)
                return;

            var canvas = EnsureOverlayCanvas();
            Transform panel = canvas.transform.Find("Panel");
            Transform parent = panel != null ? panel : canvas.transform;

            var card = CreateCard(parent, new Vector2(760, 280));
            var cardRect = card.GetComponent<RectTransform>();
            cardRect.anchorMin = new Vector2(0.5f, 0f);
            cardRect.anchorMax = new Vector2(0.5f, 0f);
            cardRect.pivot = new Vector2(0.5f, 0f);
            cardRect.anchoredPosition = new Vector2(0f, 40f);

            history.historyText = CreateText("SessionHistoryText", card.transform, "Recent Sessions", 18, TextAlignmentOptions.TopLeft);
            var textRect = history.historyText.GetComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0.06f, 0.08f);
            textRect.anchorMax = new Vector2(0.94f, 0.92f);
            textRect.offsetMin = textRect.offsetMax = Vector2.zero;
        }

        public static void EnsureAdminUI(AdminDashboardManager admin)
        {
            if (admin.metricsText != null && admin.backToLoginButton != null)
                return;

            var canvas = EnsureOverlayCanvas();
            var card = CreateCard(canvas.transform, new Vector2(900, 700));

            admin.metricsText = CreateText("AdminMetricsText", card.transform, "", 22, TextAlignmentOptions.TopLeft);
            var metricsRect = admin.metricsText.GetComponent<RectTransform>();
            metricsRect.anchorMin = new Vector2(0.08f, 0.2f);
            metricsRect.anchorMax = new Vector2(0.92f, 0.9f);
            metricsRect.offsetMin = metricsRect.offsetMax = Vector2.zero;

            admin.backToLoginButton = CreateButton("BackToLoginButton", card.transform, "Back to Login");
            var backRect = admin.backToLoginButton.GetComponent<RectTransform>();
            backRect.anchorMin = backRect.anchorMax = new Vector2(0.5f, 0.08f);
            backRect.sizeDelta = new Vector2(240, 54);
        }

        private static GameObject CreateCard(Transform parent, Vector2 size)
        {
            var card = new GameObject("Card", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            card.transform.SetParent(parent, false);
            var rect = card.GetComponent<RectTransform>();
            rect.sizeDelta = size;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            card.GetComponent<Image>().color = new Color32(255, 255, 255, 245);
            var theme = card.AddComponent<ThemeableUI>();
            theme.elementType = UIElementType.CardBackground;
            theme.ApplyTheme();
            return card;
        }

        private static TextMeshProUGUI CreateText(string name, Transform parent, string text, int size, TextAlignmentOptions align)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = size;
            tmp.alignment = align;
            tmp.enableWordWrapping = true;
            var theme = go.AddComponent<ThemeableUI>();
            theme.elementType = UIElementType.BodyText;
            theme.ApplyTheme();
            return tmp;
        }

        private static Button CreateButton(string name, Transform parent, string label)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var theme = go.AddComponent<ThemeableUI>();
            theme.elementType = UIElementType.PrimaryButton;
            var textGO = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            textGO.transform.SetParent(go.transform, false);
            var textRect = textGO.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = textRect.offsetMax = Vector2.zero;
            var tmp = textGO.GetComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.alignment = TextAlignmentOptions.Center;
            var textTheme = textGO.AddComponent<ThemeableUI>();
            textTheme.elementType = UIElementType.ButtonText;
            theme.ApplyTheme();
            textTheme.ApplyTheme();
            return go.GetComponent<Button>();
        }

        private static Button CreateTextButton(string name, Transform parent, string label)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Button));
            go.transform.SetParent(parent, false);
            var textGO = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            textGO.transform.SetParent(go.transform, false);
            var textRect = textGO.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = textRect.offsetMax = Vector2.zero;
            textGO.GetComponent<TextMeshProUGUI>().text = label;
            textGO.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;
            return go.GetComponent<Button>();
        }

        private static TMP_InputField CreateInput(string name, Transform parent, string placeholder)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(TMP_InputField));
            go.transform.SetParent(parent, false);
            var input = go.GetComponent<TMP_InputField>();
            var textGO = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            textGO.transform.SetParent(go.transform, false);
            var textRect = textGO.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(10, 6);
            textRect.offsetMax = new Vector2(-10, -6);
            input.textComponent = textGO.GetComponent<TextMeshProUGUI>();
            var phGO = new GameObject("Placeholder", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            phGO.transform.SetParent(go.transform, false);
            var phRect = phGO.GetComponent<RectTransform>();
            phRect.anchorMin = Vector2.zero;
            phRect.anchorMax = Vector2.one;
            phRect.offsetMin = new Vector2(10, 6);
            phRect.offsetMax = new Vector2(-10, -6);
            phGO.GetComponent<TextMeshProUGUI>().text = placeholder;
            input.placeholder = phGO.GetComponent<TextMeshProUGUI>();
            return input;
        }

        private static void LayoutFeedback(RectTransform title, RectTransform summary, RectTransform comments, RectTransform save, RectTransform skip, RectTransform status)
        {
            title.anchorMin = new Vector2(0.08f, 0.88f);
            title.anchorMax = new Vector2(0.92f, 0.96f);
            title.offsetMin = title.offsetMax = Vector2.zero;

            summary.anchorMin = new Vector2(0.08f, 0.38f);
            summary.anchorMax = new Vector2(0.92f, 0.86f);
            summary.offsetMin = summary.offsetMax = Vector2.zero;

            comments.anchorMin = new Vector2(0.12f, 0.24f);
            comments.anchorMax = new Vector2(0.88f, 0.24f);
            comments.sizeDelta = new Vector2(0, 48);

            save.anchorMin = save.anchorMax = new Vector2(0.5f, 0.12f);
            save.sizeDelta = new Vector2(240, 54);

            skip.anchorMin = new Vector2(0.1f, 0.04f);
            skip.anchorMax = new Vector2(0.9f, 0.04f);
            skip.sizeDelta = new Vector2(0, 30);

            status.anchorMin = new Vector2(0.1f, 0.18f);
            status.anchorMax = new Vector2(0.9f, 0.18f);
            status.sizeDelta = new Vector2(0, 30);
        }
    }
}
