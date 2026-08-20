using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using PhobiaReliefTherapy.Admin;
using PhobiaReliefTherapy.Theme;

public static class AdminSceneBuilder
{
    [MenuItem("Tools/Build Admin Scene")]
    public static void BuildAdminScene()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("Cannot Build Scene", "Exit Play Mode first.", "OK");
            return;
        }

        string scenePath = "Assets/Scenes/AdminScene.unity";
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var cameraGO = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        cameraGO.tag = "MainCamera";

        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

        var canvasGO = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        var card = new GameObject("Card", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        card.transform.SetParent(canvasGO.transform, false);
        card.GetComponent<RectTransform>().sizeDelta = new Vector2(900, 700);
        var cardTheme = card.AddComponent<ThemeableUI>();
        cardTheme.elementType = UIElementType.CardBackground;
        cardTheme.ApplyTheme();

        var metricsGO = new GameObject("AdminMetricsText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        metricsGO.transform.SetParent(card.transform, false);
        var metricsRect = metricsGO.GetComponent<RectTransform>();
        metricsRect.anchorMin = new Vector2(0.08f, 0.2f);
        metricsRect.anchorMax = new Vector2(0.92f, 0.9f);
        metricsRect.offsetMin = metricsRect.offsetMax = Vector2.zero;
        var metricsTheme = metricsGO.AddComponent<ThemeableUI>();
        metricsTheme.elementType = UIElementType.BodyText;
        metricsTheme.ApplyTheme();

        var backGO = new GameObject("BackToLoginButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Button));
        backGO.transform.SetParent(card.transform, false);
        var backRect = backGO.GetComponent<RectTransform>();
        backRect.anchorMin = backRect.anchorMax = new Vector2(0.5f, 0.08f);
        backRect.sizeDelta = new Vector2(240, 54);
        var backText = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        backText.transform.SetParent(backGO.transform, false);
        backText.GetComponent<TextMeshProUGUI>().text = "Back to Login";
        backText.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;

        var managerGO = new GameObject("AdminDashboard", typeof(AdminDashboardManager));
        var manager = managerGO.GetComponent<AdminDashboardManager>();
        manager.metricsText = metricsGO.GetComponent<TextMeshProUGUI>();
        manager.backToLoginButton = backGO.GetComponent<Button>();

        EditorSceneManager.SaveScene(scene, scenePath);
        EditorUtility.DisplayDialog("Admin Scene", "AdminScene created.", "OK");
    }
}
