using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using PhobiaReliefTherapy.Therapy;
using PhobiaReliefTherapy.Theme;

public static class DarknessSceneBuilder
{
    [MenuItem("Tools/Build Darkness Scene")]
    public static void BuildDarknessScene()
    {
        string scenePath = "Assets/Scenes/DarknessScene.unity";
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

        // Find or create Main Camera
        GameObject cameraGO = GameObject.FindWithTag("MainCamera");
        if (cameraGO == null)
        {
            cameraGO = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            cameraGO.tag = "MainCamera";
        }
        Camera camera = cameraGO.GetComponent<Camera>();
        camera.clearFlags = CameraClearFlags.Skybox;
        cameraGO.transform.position = new Vector3(0, 1, -10);

        // Find or create Canvas
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
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
        }
        else
        {
            canvasGO = canvas.gameObject;
        }

        // Ensure EventSystem
        if (Object.FindObjectOfType<EventSystem>() == null)
        {
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }

        // Find or create DarknessManager
        DarknessManager manager = Object.FindObjectOfType<DarknessManager>();
        GameObject managerGO;
        if (manager == null)
        {
            managerGO = new GameObject("DarknessManager", typeof(DarknessManager));
            manager = managerGO.GetComponent<DarknessManager>();
        }
        else
        {
            managerGO = manager.gameObject;
        }

        // Clean up children of Canvas to ensure a clean slate
        for (int i = canvasGO.transform.childCount - 1; i >= 0; i--)
        {
            Object.DestroyImmediate(canvasGO.transform.GetChild(i).gameObject);
        }

        // Create Panel (acts as root UI container)
        GameObject panelGO = new GameObject("Panel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        panelGO.transform.SetParent(canvasGO.transform, false);
        RectTransform panelRect = panelGO.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;
        
        var panelImage = panelGO.GetComponent<Image>();
        panelImage.color = new Color(0, 0, 0, 0); // transparent background so 3D skybox is visible
        panelImage.raycastTarget = false; // MUST be false so full-screen panel doesn't block camera mouse look drags!

        // 1. Create TimerText in Top-Right Corner
        GameObject timerGO = new GameObject("TimerText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        timerGO.transform.SetParent(panelGO.transform, false);
        
        RectTransform timerRect = timerGO.GetComponent<RectTransform>();
        timerRect.anchorMin = new Vector2(1f, 1f);
        timerRect.anchorMax = new Vector2(1f, 1f);
        timerRect.pivot = new Vector2(1f, 1f);
        timerRect.anchoredPosition = new Vector2(-50f, -50f);
        timerRect.sizeDelta = new Vector2(300f, 80f);

        TextMeshProUGUI timerText = timerGO.GetComponent<TextMeshProUGUI>();
        timerText.text = "30s";
        timerText.alignment = TextAlignmentOptions.TopRight;
        
        ThemeableUI timerTheme = timerGO.AddComponent<ThemeableUI>();
        timerTheme.elementType = UIElementType.HeadingText;

        // 2. Create ReturnButton in Top-Left Corner
        GameObject buttonGO = new GameObject("ReturnButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        buttonGO.transform.SetParent(panelGO.transform, false);
        
        RectTransform btnRect = buttonGO.GetComponent<RectTransform>();
        btnRect.anchorMin = new Vector2(0f, 1f);
        btnRect.anchorMax = new Vector2(0f, 1f);
        btnRect.pivot = new Vector2(0f, 1f);
        btnRect.anchoredPosition = new Vector2(50f, -50f);
        btnRect.sizeDelta = new Vector2(220f, 55f);

        Button returnButton = buttonGO.GetComponent<Button>();
        ThemeableUI btnTheme = buttonGO.AddComponent<ThemeableUI>();
        btnTheme.elementType = UIElementType.PrimaryButton;

        // ButtonText child
        GameObject btnTextGO = new GameObject("ButtonText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        btnTextGO.transform.SetParent(buttonGO.transform, false);
        
        RectTransform btnTextRect = btnTextGO.GetComponent<RectTransform>();
        btnTextRect.anchorMin = Vector2.zero;
        btnTextRect.anchorMax = Vector2.one;
        btnTextRect.offsetMin = Vector2.zero;
        btnTextRect.offsetMax = Vector2.zero;

        TextMeshProUGUI btnText = btnTextGO.GetComponent<TextMeshProUGUI>();
        btnText.text = "Return to Menu";
        btnText.alignment = TextAlignmentOptions.Center;

        ThemeableUI btnTextTheme = btnTextGO.AddComponent<ThemeableUI>();
        btnTextTheme.elementType = UIElementType.ButtonText;

        // Create and save DarknessSkybox.mat in Editor to force-include shader in the build
        string matPath = "Assets/Materials/DarknessSkybox.mat";
        Material skyboxMat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (skyboxMat == null)
        {
            Shader skyboxShader = Shader.Find("Skybox/Panoramic");
            if (skyboxShader == null) skyboxShader = Shader.Find("Legacy Shaders/Skybox/Panoramic");
            if (skyboxShader != null)
            {
                skyboxMat = new Material(skyboxShader);
                AssetDatabase.CreateAsset(skyboxMat, matPath);
            }
        }

        if (skyboxMat != null)
        {
            Texture2D darknessTexture = Resources.Load<Texture2D>("darkness_image");
            if (darknessTexture != null)
            {
                skyboxMat.SetTexture("_MainTex", darknessTexture);
                skyboxMat.SetFloat("_Mapping", 1.0f);    // Latitude Longitude
                skyboxMat.SetFloat("_ImageType", 0.0f);  // 360 Degrees
                skyboxMat.SetFloat("_Exposure", 1.0f);
                EditorUtility.SetDirty(skyboxMat);
            }
            manager.darknessSkyboxMaterial = skyboxMat;
        }

        // Assign script field references
        manager.timerText = timerText;
        manager.returnButton = returnButton;

        // Apply theme styling in Edit Mode
        timerTheme.ApplyTheme();
        btnTheme.ApplyTheme();
        btnTextTheme.ApplyTheme();

        // Save scene modifications
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Debug.Log("Successfully built DarknessScene hierarchy and saved scene.");
    }
}
