using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using PhobiaReliefTherapy.Therapy;
using PhobiaReliefTherapy.Theme;

public static class FeedbackSceneBuilder
{
    [MenuItem("Tools/Build Feedback Scene")]
    public static void BuildFeedbackScene()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("Cannot Build Scene", "Exit Play Mode first.", "OK");
            return;
        }

        string scenePath = "Assets/Scenes/FeedbackScene.unity";
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

        GameObject cameraGO = GameObject.FindWithTag("MainCamera");
        if (cameraGO == null)
        {
            cameraGO = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            cameraGO.tag = "MainCamera";
        }
        cameraGO.GetComponent<Camera>().clearFlags = CameraClearFlags.SolidColor;

        if (Object.FindObjectOfType<EventSystem>() == null)
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

        Canvas canvas = Object.FindObjectOfType<Canvas>();
        GameObject canvasGO;
        if (canvas == null)
        {
            canvasGO = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
        }
        else
        {
            canvasGO = canvas.gameObject;
        }

        for (int i = canvasGO.transform.childCount - 1; i >= 0; i--)
            Object.DestroyImmediate(canvasGO.transform.GetChild(i).gameObject);

        GameObject card = CreateCard(canvasGO.transform, new Vector2(900, 720));
        GameObject title = CreateText("FeedbackTitle", card.transform, "Session Feedback", 42, TextAlignmentOptions.Center);
        GameObject summary = CreateText("FeedbackSummaryText", card.transform, "", 20, TextAlignmentOptions.TopLeft);
        GameObject comments = CreateInput("FeedbackCommentsInput", card.transform, "Optional comments...");
        GameObject saveBtn = CreateButton("SaveFeedbackButton", card.transform, "Save & Continue");
        GameObject skipBtn = CreateTextButton("SkipFeedbackButton", card.transform, "Skip");
        GameObject status = CreateText("FeedbackStatusText", card.transform, "", 18, TextAlignmentOptions.Center);

        LayoutFeedback(title.GetComponent<RectTransform>(), summary.GetComponent<RectTransform>(),
            comments.GetComponent<RectTransform>(), saveBtn.GetComponent<RectTransform>(),
            skipBtn.GetComponent<RectTransform>(), status.GetComponent<RectTransform>());

        FeedbackManager manager = Object.FindObjectOfType<FeedbackManager>();
        GameObject managerGO;
        if (manager == null)
        {
            managerGO = new GameObject("FeedbackManager", typeof(FeedbackManager));
            manager = managerGO.GetComponent<FeedbackManager>();
        }
        else managerGO = manager.gameObject;

        manager.titleText = title.GetComponent<TextMeshProUGUI>();
        manager.summaryText = summary.GetComponent<TextMeshProUGUI>();
        manager.commentsInput = comments.GetComponent<TMP_InputField>();
        manager.saveButton = saveBtn.GetComponent<Button>();
        manager.skipButton = skipBtn.GetComponent<Button>();
        manager.statusText = status.GetComponent<TextMeshProUGUI>();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, scenePath);
        EditorUtility.DisplayDialog("Feedback Scene", "FeedbackScene rebuilt.", "OK");
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

    private static GameObject CreateText(string name, Transform parent, string text, int size, TextAlignmentOptions align)
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
        return go;
    }

    private static GameObject CreateButton(string name, Transform parent, string label)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        var theme = go.AddComponent<ThemeableUI>();
        theme.elementType = UIElementType.PrimaryButton;
        var textGO = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textGO.transform.SetParent(go.transform, false);
        var tmp = textGO.GetComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.alignment = TextAlignmentOptions.Center;
        var textTheme = textGO.AddComponent<ThemeableUI>();
        textTheme.elementType = UIElementType.ButtonText;
        theme.ApplyTheme();
        textTheme.ApplyTheme();
        return go;
    }

    private static GameObject CreateTextButton(string name, Transform parent, string label)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Button));
        go.transform.SetParent(parent, false);
        var textGO = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textGO.transform.SetParent(go.transform, false);
        textGO.GetComponent<TextMeshProUGUI>().text = label;
        return go;
    }

    private static GameObject CreateInput(string name, Transform parent, string placeholder)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(TMP_InputField));
        go.transform.SetParent(parent, false);
        var input = go.GetComponent<TMP_InputField>();
        var textGO = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textGO.transform.SetParent(go.transform, false);
        input.textComponent = textGO.GetComponent<TextMeshProUGUI>();
        var phGO = new GameObject("Placeholder", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        phGO.transform.SetParent(go.transform, false);
        phGO.GetComponent<TextMeshProUGUI>().text = placeholder;
        input.placeholder = phGO.GetComponent<TextMeshProUGUI>();
        return go;
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
