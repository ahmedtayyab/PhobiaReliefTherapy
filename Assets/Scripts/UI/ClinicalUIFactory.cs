using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using PhobiaReliefTherapy;
using PhobiaReliefTherapy.Theme;
using PhobiaReliefTherapy.Managers;
using PhobiaReliefTherapy.VR;

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
            image.color = new Color(0.07f, 0.08f, 0.10f, 0.92f);
            image.raycastTarget = true;

            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(RectMask2D));
            viewport.transform.SetParent(scrollGO.transform, false);
            var viewportRect = viewport.GetComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = new Vector2(8f, 8f);
            viewportRect.offsetMax = new Vector2(-68f, -8f);
            var viewportImage = viewport.GetComponent<Image>();
            viewportImage.color = new Color(1f, 1f, 1f, 0.02f);
            viewportImage.raycastTarget = true;

            var content = new GameObject("Content", typeof(RectTransform));
            content.transform.SetParent(viewport.transform, false);
            var contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = new Vector2(0f, 40f);

            body = CreateText(name + "Body", content.transform, "", 17, TextAlignmentOptions.TopLeft);
            var bodyRect = body.rectTransform;
            bodyRect.anchorMin = new Vector2(0f, 1f);
            bodyRect.anchorMax = new Vector2(1f, 1f);
            bodyRect.pivot = new Vector2(0.5f, 1f);
            bodyRect.offsetMin = new Vector2(10f, 0f);
            bodyRect.offsetMax = new Vector2(-10f, 0f);
            body.enableWordWrapping = true;
            body.overflowMode = TextOverflowModes.Overflow;
            body.raycastTarget = false;

            var barGO = new GameObject("Scrollbar", typeof(RectTransform), typeof(Image), typeof(Scrollbar));
            barGO.transform.SetParent(scrollGO.transform, false);
            var barRect = barGO.GetComponent<RectTransform>();
            barRect.anchorMin = new Vector2(1f, 0f);
            barRect.anchorMax = new Vector2(1f, 1f);
            barRect.pivot = new Vector2(1f, 0.5f);
            barRect.sizeDelta = new Vector2(56f, -16f);
            barRect.anchoredPosition = new Vector2(-8f, 0f);
            barGO.GetComponent<Image>().color = new Color(0.17f, 0.19f, 0.23f, 1f);
            barGO.GetComponent<Image>().raycastTarget = true;

            var sliding = new GameObject("Sliding Area", typeof(RectTransform));
            sliding.transform.SetParent(barGO.transform, false);
            Stretch(sliding.GetComponent<RectTransform>(), 4f, 8f);

            var handle = new GameObject("Handle", typeof(RectTransform), typeof(Image));
            handle.transform.SetParent(sliding.transform, false);
            Stretch(handle.GetComponent<RectTransform>());
            handle.GetComponent<Image>().color = new Color(1f, 87f / 255f, 34f / 255f, 1f);
            handle.GetComponent<Image>().raycastTarget = true;

            var bar = barGO.GetComponent<Scrollbar>();
            bar.handleRect = handle.GetComponent<RectTransform>();
            bar.direction = Scrollbar.Direction.BottomToTop;
            bar.targetGraphic = handle.GetComponent<Image>();
            bar.interactable = true;
            bar.size = 0.35f;

            var scroll = scrollGO.GetComponent<ScrollRect>();
            scroll.viewport = viewportRect;
            scroll.content = contentRect;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.inertia = true;
            scroll.scrollSensitivity = 40f;
            scroll.verticalScrollbar = bar;
            scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
            scroll.verticalScrollbarSpacing = 6f;
            scrollGO.AddComponent<VrScrollRelay>();
            return scroll;
        }

        public static void ApplyScrollBody(TextMeshProUGUI body, string text)
        {
            if (body == null)
                return;

            body.text = text ?? "";
            body.enableWordWrapping = true;
            body.overflowMode = TextOverflowModes.Overflow;
            body.ForceMeshUpdate();
            float width = Mathf.Max(64f, body.rectTransform.rect.width);
            float height = Mathf.Max(body.GetPreferredValues(width, 0f).y, body.preferredHeight) + 24f;
            body.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
            var content = body.transform.parent as RectTransform;
            if (content != null)
                content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
            Canvas.ForceUpdateCanvases();

            var relay = body.GetComponentInParent<VrScrollRelay>();
            if (relay != null)
                relay.MarkDirty();
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
