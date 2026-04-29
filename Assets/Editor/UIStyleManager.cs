using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using PhobiaReliefTherapy.Theme;

public class UIStyleManager : Editor
{
    [MenuItem("Tools/Apply Global Theme to CURRENT Scene")]
    public static void ApplyToCurrentScene()
    {
        ProcessScene(EditorSceneManager.GetActiveScene());
        Debug.Log("⚕️ Medical Theme applied to current scene!");
    }

    [MenuItem("Tools/Apply Global Theme to ALL Build Scenes (Ultimate Tool)")]
    public static void ApplyToAllScenes()
    {
        if (!EditorUtility.DisplayDialog("Apply Globally?", "This will automatically open EVERY scene in your Build Settings, apply the strict Medical Theme, and save them. Proceed?", "Yes, Apply Everywhere", "Cancel"))
        {
            return;
        }

        // Iterate through all scenes added to Build Settings
        foreach (var sceneAsset in EditorBuildSettings.scenes)
        {
            if (sceneAsset.enabled)
            {
                Scene scene = EditorSceneManager.OpenScene(sceneAsset.path, OpenSceneMode.Single);
                ProcessScene(scene);
            }
        }
        
        Debug.Log("⚕️ ULTIMATE MEDICAL THEME APPLIED TO ALL SCENES!");
    }

    private static void ProcessScene(Scene scene)
    {
        // Check if theme asset exists in Resources
        ThemePreset theme = Resources.Load<ThemePreset>("MedicalTheme");
        if (theme == null)
        {
            Debug.LogError("Could not find 'MedicalTheme.asset' in the Assets/Resources folder! Please make sure it is named exactly MedicalTheme.");
            return;
        }

        // 1. FIX BLURRY CANVAS
        CanvasScaler[] scalers = Object.FindObjectsOfType<CanvasScaler>();
        foreach (var scaler in scalers)
        {
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            EditorUtility.SetDirty(scaler);
        }

        // 2. PURGE OLD PROCEDURAL STYLES
        Component[] allComps = Object.FindObjectsOfType<Component>();
        foreach (var c in allComps)
        {
            if (c.GetType().Name == "UIGradient")
            {
                DestroyImmediate(c);
            }
        }

        // 3. ATTACH THEMEABLE UI TO IMAGES
        Image[] images = Object.FindObjectsOfType<Image>();
        foreach (var img in images)
        {
            ThemeableUI tui = img.GetComponent<ThemeableUI>();
            if (tui == null) tui = img.gameObject.AddComponent<ThemeableUI>();

            if (img.GetComponent<Button>() != null)
            {
                tui.elementType = UIElementType.PrimaryButton;
            }
            else if (img.GetComponentInParent<TMP_InputField>() != null)
            {
                tui.elementType = UIElementType.InputField;
            }
            else if (img.gameObject.name.ToLower().Contains("card"))
            {
                tui.elementType = UIElementType.CardBackground;
            }
            else if (img.gameObject.name.ToLower() == "panel" && img.transform.parent != null && img.transform.parent.GetComponent<Canvas>() != null)
            {
                tui.elementType = UIElementType.ScreenBackground;
            }

            tui.ApplyTheme();
            EditorUtility.SetDirty(tui);
        }

        // 4. ATTACH THEMEABLE UI TO TEXTS
        TextMeshProUGUI[] texts = Object.FindObjectsOfType<TextMeshProUGUI>();
        foreach (var txt in texts)
        {
            ThemeableUI tui = txt.GetComponent<ThemeableUI>();
            if (tui == null) tui = txt.gameObject.AddComponent<ThemeableUI>();

            if (txt.gameObject.name.ToLower().Contains("error"))
            {
                tui.elementType = UIElementType.ErrorText;
            }
            else if (txt.GetComponentInParent<Button>() != null)
            {
                tui.elementType = UIElementType.ButtonText;
            }
            else if (txt.gameObject.name.ToLower().Contains("title") || txt.fontSize >= 30)
            {
                tui.elementType = UIElementType.HeadingText;
            }
            else if (txt.gameObject.name.ToLower().Contains("label") || txt.gameObject.name.ToLower().Contains("placeholder") || txt.gameObject.name.ToLower().Contains("text area"))
            {
                tui.elementType = UIElementType.LabelText;
            }
            else
            {
                tui.elementType = UIElementType.BodyText;
            }

            tui.ApplyTheme();
            EditorUtility.SetDirty(tui);
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }
}
