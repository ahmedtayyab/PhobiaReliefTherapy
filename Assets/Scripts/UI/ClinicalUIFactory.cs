using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using PhobiaReliefTherapy;
using PhobiaReliefTherapy.Theme;
using PhobiaReliefTherapy.Managers;

namespace PhobiaReliefTherapy.UI
{
    /// <summary>
    /// Runtime UI helpers that match the existing medical card / ThemeableUI look.
    /// </summary>
    public static class ClinicalUIFactory
    {
        public static Canvas EnsureOverlayCanvas()
        {
            if (VrAvailability.IsHeadsetPresent())
            {
                Canvas worldFirst = FindUsableCanvas(RenderMode.WorldSpace);
                if (worldFirst != null)
                    return worldFirst;
            }

            Canvas overlay = FindUsableCanvas(RenderMode.ScreenSpaceOverlay);
            if (overlay != null)
                return overlay;

            Canvas cameraCanvas = FindUsableCanvas(RenderMode.ScreenSpaceCamera);
            if (cameraCanvas != null)
                return cameraCanvas;

            Canvas worldCanvas = FindUsableCanvas(RenderMode.WorldSpace);
            if (worldCanvas != null)
                return worldCanvas;

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

        private static Canvas FindUsableCanvas(RenderMode mode)
        {
            var canvases = Object.FindObjectsOfType<Canvas>();
            foreach (var existing in canvases)
            {
                if (existing == null || existing.renderMode != mode)
                    continue;
                if (!existing.gameObject.scene.isLoaded)
                    continue;
                if (existing.GetComponentInParent<SceneLoader>() != null)
                    continue;
                if (existing.GetComponentInParent<SessionVitalsOverlay>() != null)
                    continue;
                if (existing.gameObject.name == "TransitionCanvas" ||
                    existing.gameObject.name == "VitalsOverlayCanvas" ||
                    existing.gameObject.name == "VRKeyboard_Root" ||
                    existing.gameObject.name == "FeedbackWorldCanvas")
                    continue;
                if (existing.transform.root != null && existing.transform.root.name == "VRKeyboard_Root")
                    continue;
                return existing;
            }

            return null;
        }

        public static Transform OverlayRoot(Canvas canvas)
        {
            var panel = canvas.transform.Find("Panel");
            return panel != null ? panel : canvas.transform;
        }

        public static GameObject CreateFullScreenPanel(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            Stretch(rect);
            go.GetComponent<Image>().color = new Color(0.05f, 0.05f, 0.07f, 0.55f);
            go.GetComponent<Image>().raycastTarget = true;
            return go;
        }

        public static GameObject CreateCard(Transform parent, Vector2 size, string name = "Card")
        {
            var card = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            card.transform.SetParent(parent, false);
            var rect = card.GetComponent<RectTransform>();
            rect.sizeDelta = size;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            var theme = card.AddComponent<ThemeableUI>();
            theme.elementType = UIElementType.CardBackground;
            theme.ApplyTheme();
            return card;
        }

        public static TextMeshProUGUI CreateText(string name, Transform parent, string text, float size, TextAlignmentOptions align, UIElementType type = UIElementType.BodyText)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = size;
            tmp.alignment = align;
            tmp.enableWordWrapping = true;
            tmp.overflowMode = TextOverflowModes.Overflow;
            var theme = go.AddComponent<ThemeableUI>();
            theme.elementType = type;
            theme.overrideTypography = true;
            theme.ApplyTheme();
            tmp.fontSize = size;
            return tmp;
        }

        public static Button CreateButton(string name, Transform parent, string label, bool primary = true)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().raycastTarget = true;
            var theme = go.AddComponent<ThemeableUI>();
            theme.elementType = primary ? UIElementType.PrimaryButton : UIElementType.SecondaryButton;

            var textGO = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            textGO.transform.SetParent(go.transform, false);
            Stretch(textGO.GetComponent<RectTransform>(), 14f, 8f);
            var tmp = textGO.GetComponent<TextMeshProUGUI>();
            tmp.text = label;
            AlignButtonLabel(tmp);
            var textTheme = textGO.AddComponent<ThemeableUI>();
            textTheme.elementType = UIElementType.ButtonText;
            textTheme.overrideTypography = true;
            theme.ApplyTheme();
            textTheme.ApplyTheme();
            AlignButtonLabel(tmp);
            return go.GetComponent<Button>();
        }

        public static void AlignButtonLabel(TextMeshProUGUI tmp)
        {
            if (tmp == null)
                return;

            if (string.IsNullOrEmpty(tmp.text))
                return;

            if (tmp.fontSize < 14f)
                tmp.fontSize = 18f;

            tmp.alignment = TextAlignmentOptions.Center;
            tmp.verticalAlignment = VerticalAlignmentOptions.Middle;
            tmp.horizontalAlignment = HorizontalAlignmentOptions.Center;
            tmp.enableWordWrapping = false;
            tmp.overflowMode = TextOverflowModes.Overflow;
            tmp.enableAutoSizing = true;
            tmp.fontSizeMin = 14f;
            tmp.fontSizeMax = 22f;
            tmp.color = Color.white;
            tmp.raycastTarget = false;
            tmp.margin = new Vector4(4f, 2f, 4f, 2f);
        }

        public static TMP_InputField CreateInput(string name, Transform parent, string placeholder, bool password = false)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(TMP_InputField));
            go.transform.SetParent(parent, false);
            var input = go.GetComponent<TMP_InputField>();
            var theme = go.AddComponent<ThemeableUI>();
            theme.elementType = UIElementType.InputField;
            theme.ApplyTheme();

            var textGO = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            textGO.transform.SetParent(go.transform, false);
            var textRect = textGO.GetComponent<RectTransform>();
            Stretch(textRect, 12, 6);
            input.textComponent = textGO.GetComponent<TextMeshProUGUI>();
            input.textComponent.fontSize = 20;

            var phGO = new GameObject("Placeholder", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            phGO.transform.SetParent(go.transform, false);
            Stretch(phGO.GetComponent<RectTransform>(), 12, 6);
            var ph = phGO.GetComponent<TextMeshProUGUI>();
            ph.text = placeholder;
            ph.fontSize = 20;
            ph.fontStyle = FontStyles.Italic;
            input.placeholder = ph;

            if (password)
                input.contentType = TMP_InputField.ContentType.Password;

            return input;
        }

        public static ScrollRect CreateScrollText(string name, Transform parent, out TextMeshProUGUI body)
        {
            var scrollGO = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(ScrollRect));
            scrollGO.transform.SetParent(parent, false);
            var image = scrollGO.GetComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0.04f);
            image.raycastTarget = true;

            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            viewport.transform.SetParent(scrollGO.transform, false);
            Stretch(viewport.GetComponent<RectTransform>());
            viewport.GetComponent<Image>().color = new Color(1, 1, 1, 0.01f);
            viewport.GetComponent<Mask>().showMaskGraphic = false;

            var content = new GameObject("Content", typeof(RectTransform), typeof(ContentSizeFitter));
            content.transform.SetParent(viewport.transform, false);
            var contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.offsetMin = Vector2.zero;
            contentRect.offsetMax = Vector2.zero;
            content.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            body = CreateText(name + "Body", content.transform, "", 18, TextAlignmentOptions.TopLeft);
            Stretch(body.rectTransform, 8, 8);
            var layout = body.gameObject.AddComponent<ContentSizeFitter>();
            layout.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var scroll = scrollGO.GetComponent<ScrollRect>();
            scroll.viewport = viewport.GetComponent<RectTransform>();
            scroll.content = contentRect;
            scroll.horizontal = false;
            scroll.vertical = true;
            return scroll;
        }

        public static void Anchor(RectTransform rect, Vector2 min, Vector2 max, Vector2? size = null, Vector2? pos = null)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.pivot = new Vector2(0.5f, 0.5f);
            if (size.HasValue)
                rect.sizeDelta = size.Value;
            else
                rect.offsetMin = rect.offsetMax = Vector2.zero;
            rect.anchoredPosition = pos ?? Vector2.zero;
        }

        public static void Stretch(RectTransform rect, float padX = 0f, float padY = 0f)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(padX, padY);
            rect.offsetMax = new Vector2(-padX, -padY);
        }
    }
}
