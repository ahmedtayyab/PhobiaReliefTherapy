#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace PhobiaReliefTherapy.UI
{
    public static class PhobiaUIBuilder
    {
        private static readonly string[] ScenePaths = new[]
        {
            "Assets/Scenes/WelcomeScene.unity",
            "Assets/Scenes/LoginScene.unity",
            "Assets/Scenes/RegisterScene.unity",
            "Assets/Scenes/DashboardScene.unity",
            "Assets/Scenes/BaselineScene.unity",
            "Assets/Scenes/PhobiaSelectionScene.unity",
            "Assets/Scenes/CrowdScene.unity",
            "Assets/Scenes/DarknessScene.unity",
            "Assets/Scenes/FeedbackScene.unity",
            "Assets/Scenes/HeightScene.unity",
            "Assets/Scenes/SafeRoomScene.unity"
        };

        private const string DefaultStyleGuidePath = "Assets/ScriptableObjects/UIStyleGuide.asset";

        [MenuItem("Tools/Phobia Relief/Create UI Style Guide Asset")]
        public static void CreateStyleGuideAssetMenu()
        {
            var style = LoadStyleGuide();
            if (style != null)
            {
                Debug.Log("UIStyleGuide already exists at: " + AssetDatabase.GetAssetPath(style));
                return;
            }

            style = CreateDefaultStyleGuideAsset();
            if (style != null)
            {
                Debug.Log("Created default UIStyleGuide asset at " + DefaultStyleGuidePath + ". Assign ModernFont and optionally rounded sprite before building prefabs.");
            }
        }

        [MenuItem("Tools/Phobia Relief/Create UI Prefabs")]
        public static void CreateUIPrefabs()
        {
            var style = LoadStyleGuide();
            if (style == null)
            {
                Debug.LogError("UI Style Guide asset not found. Create it first in Assets/ScriptableObjects or Assets/Scenes.");
                return;
            }

            EnsureFolder("Assets/Prefabs/UI");
            CreateBackgroundPrefab(style);
            CreateCardPrefab(style);
            CreateButtonPrefab(style);
            CreateInputFieldPrefab(style);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Phobia Relief UI prefabs created in Assets/Prefabs/UI.");
        }

        [MenuItem("Tools/Phobia Relief/Apply UI Style To Current Scene")]
        public static void ApplyStyleToCurrentScene()
        {
            var style = LoadStyleGuide();
            if (style == null)
            {
                Debug.LogError("UI Style Guide asset not found.");
                return;
            }

            var scene = EditorSceneManager.GetActiveScene();
            var canvas = Object.FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                Debug.LogError("No Canvas found in the current scene.");
                return;
            }

            ApplyStyleToCanvas(canvas, style);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"Applied UI style to current scene: {scene.name}");
        }

        [MenuItem("Tools/Phobia Relief/Apply UI Style To All Scenes")]
        public static void ApplyStyleToAllScenes()
        {
            var style = LoadStyleGuide();
            if (style == null)
            {
                Debug.LogError("UI Style Guide asset not found.");
                return;
            }

            CreateUIPrefabs();

            foreach (var scenePath in ScenePaths)
            {
                if (!File.Exists(scenePath))
                {
                    Debug.LogWarning($"Scene not found: {scenePath}");
                    continue;
                }

                var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                var canvas = Object.FindObjectOfType<Canvas>();
                if (canvas == null)
                {
                    canvas = CreateCanvas();
                }

                ApplyStyleToCanvas(canvas, style);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log($"Applied UI style to: {scenePath}");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        [MenuItem("Tools/Phobia Relief/Build UI For All Scenes")]
        public static void BuildUIForAllScenes()
        {
            var style = LoadStyleGuide();
            if (style == null)
            {
                Debug.LogError("UI Style Guide asset not found. Create it first.");
                return;
            }

            CreateUIPrefabs();

            foreach (var scenePath in ScenePaths)
            {
                if (!File.Exists(scenePath))
                {
                    Debug.LogWarning($"Scene not found: {scenePath}");
                    continue;
                }

                var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                BuildSceneUI(scene, style);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log($"Updated scene UI: {scenePath}");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void EnsureCamera(UIStyleGuide style)
        {
            var cameras = Object.FindObjectsOfType<Camera>();
            if (cameras.Length == 0)
            {
                var cameraGO = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
                cameraGO.tag = "MainCamera";
                cameraGO.transform.position = new Vector3(0, 0, -10);
                cameraGO.transform.rotation = Quaternion.identity;
                
                var cam = cameraGO.GetComponent<Camera>();
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = style.backgroundColor;
                Debug.Log("Created missing Main Camera in scene.");
            }
            else
            {
                foreach (var cam in cameras)
                {
                    cam.clearFlags = CameraClearFlags.SolidColor;
                    cam.backgroundColor = style.backgroundColor;
                }
            }
        }

        private static void ApplyStyleToCanvas(Canvas canvas, UIStyleGuide style)
        {
            if (canvas == null || style == null)
                return;

            EnsureCamera(style);
            SetupCanvasScaler(canvas);
            var root = canvas.transform;

            var background = FindChildRecursive(root, "UI_Background");
            if (background == null)
            {
                background = InstantiatePrefabAtPath("Assets/Prefabs/UI/UI_Background.prefab", root);
            }
            
            if (background != null)
            {
                var image = background.GetComponent<Image>();
                if (image != null)
                {
                    if (style.useGradients)
                    {
                        image.color = Color.white;
                        var grad = background.GetComponent<UIGradient>();
                        if (grad == null) grad = background.gameObject.AddComponent<UIGradient>();
                        grad.topColor = style.screenBackgroundGradientTop;
                        grad.bottomColor = style.screenBackgroundGradientBottom;
                        grad.vertical = true;
                        image.SetVerticesDirty();
                    }
                    else
                    {
                        var grad = background.GetComponent<UIGradient>();
                        if (grad != null) Object.DestroyImmediate(grad);
                        image.color = style.backgroundColor;
                    }
                    image.type = Image.Type.Simple;
                }
                background.transform.SetAsFirstSibling();
            }

            var card = FindChildRecursive(root, "UI_Card");
            bool newlyCreatedCard = false;
            if (card == null)
            {
                card = InstantiatePrefabAtPath("Assets/Prefabs/UI/UI_Card.prefab", root);
                newlyCreatedCard = true;
            }

            if (card != null)
            {
                StyleCard(card, style);
                card.transform.SetAsLastSibling();
                EnsureCardTransform(card);
            }

            if (newlyCreatedCard)
            {
                CleanupOldUI(root, card?.transform);
                MoveOldUIIntoCard(root, card?.transform);
            }

            HideOldBackgroundPanels(root, card?.transform);
            StyleAllUI(root, style);

            if (card != null)
            {
                var rt = card.GetComponent<RectTransform>();
                if (rt != null)
                {
                    UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
                }
                EnsureHeading(card.transform, style, canvas.gameObject.scene.name);
            }
        }

        private static void SetupCanvasScaler(Canvas canvas)
        {
            var scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler == null)
            {
                scaler = canvas.gameObject.AddComponent<CanvasScaler>();
            }

            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
        }

        private static void EnsureCardTransform(GameObject card)
        {
            if (card == null)
                return;

            var rt = card.GetComponent<RectTransform>();
            if (rt == null)
                return;

            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(760, 520);
        }

        private static void StyleCard(GameObject card, UIStyleGuide style)
        {
            if (card == null || style == null)
                return;

            var image = card.GetComponent<Image>();
            if (image == null)
                image = card.AddComponent<Image>();

            image.sprite = style.GetRoundedSprite(image.sprite);
            image.type = Image.Type.Sliced;
            
            if (style.useGradients)
            {
                image.color = Color.white;
                var grad = card.GetComponent<UIGradient>();
                if (grad == null) grad = card.AddComponent<UIGradient>();
                grad.topColor = style.cardBackgroundGradientTop;
                grad.bottomColor = style.cardBackgroundGradientBottom;
                grad.vertical = true;
                image.SetVerticesDirty();
            }
            else
            {
                var grad = card.GetComponent<UIGradient>();
                if (grad != null) Object.DestroyImmediate(grad);
                image.color = style.cardColor;
            }
            image.raycastTarget = true;

            var layout = card.GetComponent<VerticalLayoutGroup>();
            if (layout == null)
                layout = card.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.padding = new RectOffset((int)style.cardPadding, (int)style.cardPadding, (int)style.cardPadding, (int)style.cardPadding);
            layout.spacing = (int)style.elementSpacing;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            var fitter = card.GetComponent<ContentSizeFitter>();
            if (fitter == null)
                fitter = card.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        private static void MoveOldUIIntoCard(Transform root, Transform cardTransform)
        {
            if (root == null || cardTransform == null)
                return;

            var list = new List<Transform>();
            var containersToDestroy = new List<GameObject>();

            foreach (Transform child in root)
            {
                if (child == cardTransform || child.name == "UI_Background" || child.GetComponent<UnityEngine.EventSystems.EventSystem>() != null)
                    continue;

                if (child.GetComponent<Canvas>() != null)
                    continue;

                var lowerName = child.name.ToLower();
                bool isContainer = lowerName.Contains("card") || lowerName.Contains("panel") || lowerName.Contains("background") || lowerName.Contains("container") || lowerName.Contains("frame");

                if (isContainer && child.childCount > 0)
                {
                    var subChildren = new List<Transform>();
                    foreach (Transform sc in child)
                    {
                        subChildren.Add(sc);
                    }
                    foreach (var sc in subChildren)
                    {
                        list.Add(sc);
                    }
                    containersToDestroy.Add(child.gameObject);
                }
                else
                {
                    if (child.GetComponent<Graphic>() == null && child.childCount == 0)
                        continue;

                    list.Add(child);
                }
            }

            foreach (var child in list)
            {
                child.SetParent(cardTransform, false);
                var rt = child.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.anchorMin = new Vector2(0.5f, 0.5f);
                    rt.anchorMax = new Vector2(0.5f, 0.5f);
                    rt.anchoredPosition = Vector2.zero;
                }
            }

            foreach (var container in containersToDestroy)
            {
                if (container != null)
                    Object.DestroyImmediate(container);
            }
        }

        private static void CleanupOldUI(Transform root, Transform cardTransform)
        {
            if (root == null)
                return;

            var oldUI = new List<Transform>();
            foreach (var child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child == root || child == cardTransform)
                    continue;
                if (child.name == "EventSystem")
                    continue;
                if (child.GetComponent<UnityEngine.EventSystems.EventSystem>() != null)
                    continue;
                if (child.GetComponent<Canvas>() != null)
                    continue;

                var lowerName = child.name.ToLower();
                if (lowerName.Contains("ui_background") || lowerName.Contains("ui_card") || lowerName.Contains("panel") || lowerName.Contains("background") || lowerName.Contains("container") || lowerName.Contains("frame") || lowerName.Contains("card"))
                {
                    if (child.GetComponent<Button>() == null && child.GetComponent<InputField>() == null && child.GetComponent<TMP_InputField>() == null && child.GetComponent<Text>() == null && child.GetComponent<TMP_Text>() == null)
                    {
                        oldUI.Add(child);
                    }
                }
            }

            foreach (var child in oldUI)
            {
                if (child != null)
                    Object.DestroyImmediate(child.gameObject);
            }
        }

        private static GameObject FindChildRecursive(Transform parent, string name)
        {
            if (parent == null)
                return null;

            foreach (Transform child in parent)
            {
                if (child.name == name)
                    return child.gameObject;

                var found = FindChildRecursive(child, name);
                if (found != null)
                    return found;
            }

            return null;
        }

        private static void HideOldBackgroundPanels(Transform root, Transform cardTransform)
        {
            if (root == null)
                return;

            var allImages = root.GetComponentsInChildren<Image>(true);
            foreach (var image in allImages)
            {
                if (image.gameObject.name == "UI_Background" || image.gameObject == cardTransform?.gameObject)
                    continue;

                var lowerName = image.gameObject.name.ToLower();
                if (lowerName.Contains("panel") || lowerName.Contains("background") || lowerName.Contains("container") || lowerName.Contains("frame"))
                {
                    if (image.GetComponent<Button>() == null && image.GetComponent<InputField>() == null && image.GetComponent<TMP_InputField>() == null)
                    {
                        image.color = new Color(0, 0, 0, 0);
                        image.raycastTarget = false;
                    }
                }
            }
        }

        private static void StyleAllUI(Transform root, UIStyleGuide style)
        {
            if (root == null || style == null)
                return;

            var buttons = root.GetComponentsInChildren<Button>(true);
            foreach (var button in buttons)
            {
                StyleButton(button, style);
            }

            var inputFields = root.GetComponentsInChildren<InputField>(true);
            foreach (var input in inputFields)
            {
                StyleInputField(input, style);
            }

            var tmpInputFields = root.GetComponentsInChildren<TMP_InputField>(true);
            foreach (var input in tmpInputFields)
            {
                StyleTMPInputField(input, style);
            }

            var texts = root.GetComponentsInChildren<Text>(true);
            foreach (var text in texts)
            {
                if (text.GetComponentInParent<Button>() != null || text.GetComponentInParent<InputField>() != null)
                    continue;
                if (text.transform == root)
                    continue;
                StyleText(text, style);
            }

            var tmpTexts = root.GetComponentsInChildren<TMP_Text>(true);
            foreach (var tmp in tmpTexts)
            {
                if (tmp.GetComponentInParent<Button>() != null || tmp.GetComponentInParent<TMP_InputField>() != null)
                    continue;
                if (tmp.transform == root)
                    continue;
                StyleTMPText(tmp, style);
            }
        }

        private static void StyleButton(Button button, UIStyleGuide style)
        {
            if (button == null || style == null)
                return;

            var image = button.GetComponent<Image>();
            if (image == null)
                image = button.gameObject.AddComponent<Image>();

            image.sprite = style.GetRoundedSprite(image.sprite);
            image.type = Image.Type.Sliced;
            image.color = style.primaryButtonColor;
            image.raycastTarget = true;

            style.ApplyToButton(button, image);

            var layout = button.GetComponent<LayoutElement>();
            if (layout == null)
                layout = button.gameObject.AddComponent<LayoutElement>();
            layout.preferredWidth = style.buttonWidth;
            layout.preferredHeight = style.buttonHeight;
            layout.minWidth = style.buttonWidth;
            layout.minHeight = style.buttonHeight;

            var rect = button.GetComponent<RectTransform>();
            if (rect != null)
                rect.sizeDelta = new Vector2(style.buttonWidth, style.buttonHeight);

            if (button.GetComponent<ButtonHoverEffect>() == null)
                button.gameObject.AddComponent<ButtonHoverEffect>();

            var txt = button.GetComponentInChildren<Text>();
            if (txt != null)
            {
                style.ApplyToText(txt, UITextStyle.Button);
                txt.alignment = TextAnchor.MiddleCenter;
            }

            var tmp = button.GetComponentInChildren<TMP_Text>();
            if (tmp != null)
            {
                tmp.fontSize = style.buttonFontSize;
                tmp.color = style.textPrimaryColor;
                tmp.alignment = TextAlignmentOptions.Center;
            }
        }

        private static void StyleInputField(InputField input, UIStyleGuide style)
        {
            if (input == null || style == null)
                return;

            var image = input.GetComponent<Image>();
            if (image == null)
                image = input.gameObject.AddComponent<Image>();

            image.sprite = style.GetRoundedSprite(image.sprite);
            image.type = Image.Type.Sliced;
            
            if (style.useGradients)
            {
                image.color = Color.white;
                var grad = input.GetComponent<UIGradient>();
                if (grad == null) grad = input.gameObject.AddComponent<UIGradient>();
                grad.topColor = style.inputFieldGradientTop;
                grad.bottomColor = style.inputFieldGradientBottom;
                grad.vertical = true;
                image.SetVerticesDirty();
            }
            else
            {
                var grad = input.GetComponent<UIGradient>();
                if (grad != null) Object.DestroyImmediate(grad);
                image.color = style.inputBackgroundColor;
            }
            image.raycastTarget = true;
            input.targetGraphic = image;

            var layout = input.GetComponent<LayoutElement>();
            if (layout == null)
                layout = input.gameObject.AddComponent<LayoutElement>();
            layout.preferredHeight = style.inputHeight;

            if (input.textComponent != null)
            {
                input.textComponent.font = style.modernFont;
                input.textComponent.fontSize = style.inputFontSize;
                input.textComponent.color = style.textPrimaryColor;
                input.textComponent.alignment = TextAnchor.MiddleLeft;
            }

            if (input.placeholder is Text placeholderText)
            {
                placeholderText.font = style.modernFont;
                placeholderText.fontSize = style.inputFontSize;
                placeholderText.color = style.placeholderColor;
                placeholderText.alignment = TextAnchor.MiddleLeft;
            }

            var focusHighlight = input.GetComponent<InputFieldFocusHighlight>();
            if (focusHighlight == null)
                focusHighlight = input.gameObject.AddComponent<InputFieldFocusHighlight>();
            focusHighlight.focusColor = style.primaryButtonColor;
        }

        private static void StyleTMPInputField(TMP_InputField input, UIStyleGuide style)
        {
            if (input == null || style == null)
                return;

            var image = input.GetComponent<Image>();
            if (image == null)
                image = input.gameObject.AddComponent<Image>();

            image.sprite = style.GetRoundedSprite(image.sprite);
            image.type = Image.Type.Sliced;
            
            if (style.useGradients)
            {
                image.color = Color.white;
                var grad = input.GetComponent<UIGradient>();
                if (grad == null) grad = input.gameObject.AddComponent<UIGradient>();
                grad.topColor = style.inputFieldGradientTop;
                grad.bottomColor = style.inputFieldGradientBottom;
                grad.vertical = true;
                image.SetVerticesDirty();
            }
            else
            {
                var grad = input.GetComponent<UIGradient>();
                if (grad != null) Object.DestroyImmediate(grad);
                image.color = style.inputBackgroundColor;
            }
            image.raycastTarget = true;
            input.targetGraphic = image;

            var layout = input.GetComponent<LayoutElement>();
            if (layout == null)
                layout = input.gameObject.AddComponent<LayoutElement>();
            layout.preferredHeight = style.inputHeight;

            if (input.textComponent != null)
            {
                input.textComponent.font = style.modernFont != null ? TMP_FontAsset.CreateFontAsset(style.modernFont) : input.textComponent.font;
                input.textComponent.fontSize = style.inputFontSize;
                input.textComponent.color = style.textPrimaryColor;
                input.textComponent.alignment = TextAlignmentOptions.Left;
            }

            if (input.placeholder is TMP_Text placeholderText)
            {
                placeholderText.font = style.modernFont != null ? TMP_FontAsset.CreateFontAsset(style.modernFont) : placeholderText.font;
                placeholderText.fontSize = style.inputFontSize;
                placeholderText.color = style.placeholderColor;
                placeholderText.alignment = TextAlignmentOptions.Left;
            }

            var focusHighlight = input.GetComponent<InputFieldFocusHighlight>();
            if (focusHighlight == null)
                focusHighlight = input.gameObject.AddComponent<InputFieldFocusHighlight>();
            focusHighlight.focusColor = style.primaryButtonColor;
        }

        private static void StyleText(Text text, UIStyleGuide style)
        {
            if (text == null || style == null)
                return;

            var lower = text.gameObject.name.ToLower();
            if (lower.Contains("title") || lower.Contains("heading") || lower.Contains("login") || lower.Contains("register") || lower.Contains("select") || lower.Contains("phobia"))
            {
                style.ApplyToText(text, UITextStyle.Heading);
                text.color = style.textPrimaryColor;
            }
            else
            {
                style.ApplyToText(text, UITextStyle.Body);
                text.color = style.textSecondaryColor;
            }

            text.font = style.modernFont != null ? style.modernFont : text.font;
        }

        private static void StyleTMPText(TMP_Text tmp, UIStyleGuide style)
        {
            if (tmp == null || style == null)
                return;

            var lower = tmp.gameObject.name.ToLower();
            if (lower.Contains("title") || lower.Contains("heading") || lower.Contains("login") || lower.Contains("register") || lower.Contains("select") || lower.Contains("phobia"))
            {
                tmp.fontSize = style.headingFontSize;
                tmp.color = style.textPrimaryColor;
                tmp.fontStyle = FontStyles.Bold;
            }
            else
            {
                tmp.fontSize = style.bodyFontSize;
                tmp.color = style.textSecondaryColor;
                tmp.fontStyle = FontStyles.Normal;
            }

            if (style.modernFont != null && tmp.font == null)
            {
                tmp.font = TMP_FontAsset.CreateFontAsset(style.modernFont);
            }
        }

        private static void ApplyStyleToScene(UnityEngine.SceneManagement.Scene scene, UIStyleGuide style)
        {
            var canvas = Object.FindObjectOfType<Canvas>();
            if (canvas == null)
                canvas = CreateCanvas();

            ApplyStyleToCanvas(canvas, style);
        }

        private static void BuildSceneUI(UnityEngine.SceneManagement.Scene scene, UIStyleGuide style)
        {
            var canvas = Object.FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                canvas = CreateCanvas();
            }

            ApplyStyleToCanvas(canvas, style);
        }

        private static Canvas CreateCanvas()
        {
            var existing = Object.FindObjectOfType<Canvas>();
            if (existing != null)
                return existing;

            var canvasGO = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            var eventSystem = Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>();
            if (eventSystem == null)
            {
                var eventGO = new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem), typeof(UnityEngine.EventSystems.StandaloneInputModule));
            }

            return canvas;
        }

        private static void MoveExistingUIIntoCard(Transform canvasTransform, Transform cardTransform)
        {
            if (cardTransform == null)
                return;

            var childrenToMove = new List<Transform>();
            var childrenToDestroy = new List<Transform>();

            foreach (Transform child in canvasTransform)
            {
                if (child == cardTransform || child.name == "UI_Background")
                    continue;

                if (child.GetComponent<UnityEngine.EventSystems.EventSystem>() != null)
                    continue;

                if (child.name.ToLower().Contains("uiscript") || child.name.ToLower().Contains("scene fader"))
                    continue;

                if (ShouldDestroyOldUI(child))
                {
                    childrenToDestroy.Add(child);
                    continue;
                }

                childrenToMove.Add(child);
            }

            foreach (var child in childrenToMove)
            {
                child.SetParent(cardTransform, false);
            }

            foreach (var child in childrenToDestroy)
            {
                Object.DestroyImmediate(child.gameObject);
            }
        }

        private static bool ShouldDestroyOldUI(Transform child)
        {
            if (child == null)
                return false;

            var lowerName = child.name.ToLower();
            var hasButton = child.GetComponent<Button>() != null;
            var hasInput = child.GetComponent<InputField>() != null;
            var hasText = child.GetComponent<Text>() != null || child.GetComponent<TMP_Text>() != null;
            var hasGraphic = child.GetComponent<Graphic>() != null;

            if (lowerName.Contains("background") || lowerName.Contains("panel") || lowerName.Contains("container") || lowerName.Contains("frame") || lowerName.Contains("group"))
            {
                if (!hasButton && !hasInput && !hasText)
                    return true;
            }

            if (hasGraphic && !hasButton && !hasInput && !hasText && child.childCount == 0)
                return true;

            return false;
        }

        private static void EnsureHeading(Transform cardTransform, UIStyleGuide style, string sceneName)
        {
            if (cardTransform == null)
                return;

            var existingHeading = false;
            foreach (var text in cardTransform.GetComponentsInChildren<Text>(true))
            {
                string nameLower = text.name.ToLower();
                if (nameLower.Contains("title") || nameLower.Contains("heading") || nameLower.Contains("apptitle"))
                {
                    existingHeading = true;
                    break;
                }
            }

            if (!existingHeading)
            {
                foreach (var tmp in cardTransform.GetComponentsInChildren<TMP_Text>(true))
                {
                    string nameLower = tmp.name.ToLower();
                    if (nameLower.Contains("title") || nameLower.Contains("heading") || nameLower.Contains("apptitle"))
                    {
                        existingHeading = true;
                        break;
                    }
                }
            }

            if (!existingHeading)
            {
                var headingGO = new GameObject("HeadingText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
                headingGO.transform.SetParent(cardTransform, false);
                var heading = headingGO.GetComponent<TextMeshProUGUI>();
                heading.text = sceneName.Replace("Scene", "").Replace("Welcome", "Phobia Relief Therapy");
                if (style.modernFont != null)
                {
                    heading.font = TMP_FontAsset.CreateFontAsset(style.modernFont);
                }
                heading.fontSize = style.headingFontSize;
                heading.fontStyle = FontStyles.Bold;
                heading.color = style.textPrimaryColor;
                heading.alignment = TextAlignmentOptions.Center;
                var rt = headingGO.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(700, 80);
            }
        }

        private static GameObject InstantiatePrefabAtPath(string prefabPath, Transform parent)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                Debug.LogWarning($"Prefab not found at {prefabPath}");
                return null;
            }

            var instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            if (instance != null && parent != null)
            {
                instance.transform.SetParent(parent, false);
            }

            return instance;
        }

        private static UIStyleGuide LoadStyleGuide()
        {
            var guids = AssetDatabase.FindAssets("t:UIStyleGuide");
            if (guids.Length == 0)
                return null;
            var path = AssetDatabase.GUIDToAssetPath(guids[0]);
            return AssetDatabase.LoadAssetAtPath<UIStyleGuide>(path);
        }

        private static UIStyleGuide CreateDefaultStyleGuideAsset()
        {
            EnsureFolder("Assets/ScriptableObjects");
            var style = ScriptableObject.CreateInstance<UIStyleGuide>();
            style.backgroundColor = new Color32(15, 15, 31, 255);
            style.cardColor = new Color32(30, 30, 46, 255);
            style.inputBackgroundColor = new Color32(42, 42, 58, 255);
            style.primaryButtonColor = new Color32(74, 111, 255, 255);
            style.primaryButtonHighlightColor = new Color32(92, 129, 255, 255);
            style.primaryButtonPressedColor = new Color32(56, 86, 204, 255);
            style.textPrimaryColor = Color.white;
            style.textSecondaryColor = new Color32(160, 160, 176, 255);
            style.placeholderColor = new Color32(160, 160, 176, 255);
            style.inputSelectionColor = new Color32(74, 111, 255, 128);
            style.cardShadowColor = new Color32(0, 0, 0, 100);
            style.headingFontSize = 32;
            style.bodyFontSize = 16;
            style.buttonFontSize = 18;
            style.inputFontSize = 16;
            style.cardPadding = 24f;
            style.elementSpacing = 16f;
            style.buttonWidth = 220f;
            style.buttonHeight = 50f;
            style.buttonCornerRadius = 12f;
            style.inputHeight = 50f;
            style.inputCornerRadius = 8f;
            style.cardCornerRadius = 24f;
            style.cardShadowDistance = 8f;

            var spritePath = FindAssetPath("rounded_sprite", "t:Sprite") ?? FindAssetPath("rounded_sprite", "t:Texture2D");
            if (!string.IsNullOrEmpty(spritePath))
            {
                style.roundedSprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
            }

            AssetDatabase.CreateAsset(style, DefaultStyleGuidePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return style;
        }

        private static string FindAssetPath(string assetName, string filter)
        {
            var guids = AssetDatabase.FindAssets(assetName + " " + filter);
            if (guids.Length == 0)
                return null;
            return AssetDatabase.GUIDToAssetPath(guids[0]);
        }

        private static void EnsureFolder(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath))
                return;

            var parent = Path.GetDirectoryName(folderPath).Replace("\\", "/");
            var name = Path.GetFileName(folderPath);
            if (!AssetDatabase.IsValidFolder(parent))
            {
                EnsureFolder(parent);
            }
            AssetDatabase.CreateFolder(parent, name);
        }

        private static void CreateBackgroundPrefab(UIStyleGuide style)
        {
            var path = "Assets/Prefabs/UI/UI_Background.prefab";
            // Force rebuild prefabs
            if (File.Exists(path)) File.Delete(path);

            var go = new GameObject("UI_Background", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            var image = go.GetComponent<Image>();
            image.raycastTarget = false;
            image.type = Image.Type.Simple;

            if (style.useGradients)
            {
                image.color = Color.white;
                var grad = go.AddComponent<UIGradient>();
                grad.topColor = style.screenBackgroundGradientTop;
                grad.bottomColor = style.screenBackgroundGradientBottom;
                grad.vertical = true;
            }
            else
            {
                image.color = style.backgroundColor;
            }

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
        }

        private static void CreateCardPrefab(UIStyleGuide style)
        {
            var path = "Assets/Prefabs/UI/UI_Card.prefab";
            if (File.Exists(path)) File.Delete(path);

            var go = new GameObject("UI_Card", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Shadow), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            var image = go.GetComponent<Image>();
            image.sprite = style.roundedSprite;
            image.type = Image.Type.Sliced;
            image.raycastTarget = true;

            if (style.useGradients)
            {
                image.color = Color.white;
                var grad = go.AddComponent<UIGradient>();
                grad.topColor = style.cardBackgroundGradientTop;
                grad.bottomColor = style.cardBackgroundGradientBottom;
                grad.vertical = true;
            }
            else
            {
                image.color = style.cardColor;
            }

            var shadow = go.GetComponent<Shadow>();
            shadow.effectColor = style.cardShadowColor;
            shadow.effectDistance = new Vector2(0, -style.cardShadowDistance);

            var layout = go.GetComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.padding = new RectOffset((int)style.cardPadding, (int)style.cardPadding, (int)style.cardPadding, (int)style.cardPadding);
            layout.spacing = style.elementSpacing;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            layout.childControlWidth = false;
            layout.childControlHeight = false;

            var fitter = go.GetComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
        }

        private static void CreateButtonPrefab(UIStyleGuide style)
        {
            var path = "Assets/Prefabs/UI/UI_Button.prefab";
            if (File.Exists(path)) File.Delete(path);

            var go = new GameObject("UI_Button", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(LayoutElement));
            var image = go.GetComponent<Image>();
            image.sprite = style.roundedSprite;
            image.type = Image.Type.Sliced;

            if (style.useGradients)
            {
                image.color = Color.white;
                var grad = go.AddComponent<UIGradient>();
                grad.topColor = style.primaryButtonGradientTop;
                grad.bottomColor = style.primaryButtonGradientBottom;
                grad.vertical = true;
            }
            else
            {
                image.color = style.primaryButtonColor;
            }

            var button = go.GetComponent<Button>();
            button.transition = Selectable.Transition.ColorTint;
            var colors = button.colors;
            colors.normalColor = style.useGradients ? Color.white : style.primaryButtonColor;
            colors.highlightedColor = style.useGradients ? new Color(1f, 1f, 1f, 0.9f) : style.primaryButtonHighlightColor;
            colors.pressedColor = style.useGradients ? new Color(0.8f, 0.8f, 0.8f, 1f) : style.primaryButtonPressedColor;
            colors.disabledColor = new Color32(70, 70, 95, 255);
            colors.fadeDuration = 0.1f;
            button.colors = colors;

            var layout = go.GetComponent<LayoutElement>();
            layout.preferredWidth = style.buttonWidth;
            layout.preferredHeight = style.buttonHeight;

            var textGO = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            textGO.transform.SetParent(go.transform, false);
            var text = textGO.GetComponent<Text>();
            text.text = "Button";
            text.font = style.modernFont;
            text.fontSize = style.buttonFontSize;
            text.fontStyle = FontStyle.Bold;
            text.color = style.textPrimaryColor;
            text.alignment = TextAnchor.MiddleCenter;
            var textRT = textGO.GetComponent<RectTransform>();
            textRT.anchorMin = Vector2.zero;
            textRT.anchorMax = Vector2.one;
            textRT.offsetMin = Vector2.zero;
            textRT.offsetMax = Vector2.zero;

            var hover = go.AddComponent<ButtonHoverEffect>();

            PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
        }

        private static void CreateInputFieldPrefab(UIStyleGuide style)
        {
            var path = "Assets/Prefabs/UI/UI_InputField.prefab";
            if (File.Exists(path)) File.Delete(path);

            var go = new GameObject("UI_InputField", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(InputField), typeof(LayoutElement));
            var image = go.GetComponent<Image>();
            image.sprite = style.roundedSprite;
            image.type = Image.Type.Sliced;

            if (style.useGradients)
            {
                image.color = Color.white;
                var grad = go.AddComponent<UIGradient>();
                grad.topColor = style.inputFieldGradientTop;
                grad.bottomColor = style.inputFieldGradientBottom;
                grad.vertical = true;
            }
            else
            {
                image.color = style.inputBackgroundColor;
            }

            var input = go.GetComponent<InputField>();
            input.targetGraphic = image;
            input.textComponent = null;

            var layout = go.GetComponent<LayoutElement>();
            layout.preferredHeight = style.inputHeight;

            var textGO = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            textGO.transform.SetParent(go.transform, false);
            var text = textGO.GetComponent<Text>();
            text.text = string.Empty;
            text.font = style.modernFont;
            text.fontSize = style.inputFontSize;
            text.color = style.textPrimaryColor;
            text.alignment = TextAnchor.MiddleLeft;
            var textRT = textGO.GetComponent<RectTransform>();
            textRT.anchorMin = Vector2.zero;
            textRT.anchorMax = Vector2.one;
            textRT.offsetMin = new Vector2(16, 0);
            textRT.offsetMax = new Vector2(-16, 0);
            input.textComponent = text;

            var placeholderGO = new GameObject("Placeholder", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            placeholderGO.transform.SetParent(go.transform, false);
            var placeholder = placeholderGO.GetComponent<Text>();
            placeholder.text = "Enter text...";
            placeholder.font = style.modernFont;
            placeholder.fontSize = style.inputFontSize;
            placeholder.color = style.placeholderColor;
            placeholder.alignment = TextAnchor.MiddleLeft;
            var placeholderRT = placeholderGO.GetComponent<RectTransform>();
            placeholderRT.anchorMin = Vector2.zero;
            placeholderRT.anchorMax = Vector2.one;
            placeholderRT.offsetMin = new Vector2(16, 0);
            placeholderRT.offsetMax = new Vector2(-16, 0);
            input.placeholder = placeholder;

            var focusHighlight = go.AddComponent<InputFieldFocusHighlight>();
            focusHighlight.focusColor = style.primaryButtonColor;

            PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
        }
    }
}
#endif