using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

namespace PhobiaReliefTherapy.Theme
{
    [DefaultExecutionOrder(-100)]
    public class UIThemeAutoApply : MonoBehaviour
    {
        private static UIThemeAutoApply instance;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            if (instance != null)
                return;

            GameObject controller = new GameObject("[UIThemeAutoApply]");
            DontDestroyOnLoad(controller);
            instance = controller.AddComponent<UIThemeAutoApply>();
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void Start()
        {
            ApplyThemeToScene(SceneManager.GetActiveScene());
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            ApplyThemeToScene(scene);
        }

        private void ApplyThemeToScene(Scene scene)
        {
            ThemePreset theme = Resources.Load<ThemePreset>("MedicalTheme");
            if (theme == null)
                return;

            ApplyThemeToImages(theme);
            ApplyThemeToTexts(theme);
        }

        private void ApplyThemeToImages(ThemePreset theme)
        {
            foreach (var image in FindObjectsOfType<Image>(true))
            {
                ThemeableUI themeable = image.GetComponent<ThemeableUI>();
                if (themeable == null)
                    themeable = image.gameObject.AddComponent<ThemeableUI>();

                themeable.elementType = ResolveImageElementType(image);
                themeable.ApplyTheme();
            }
        }

        private void ApplyThemeToTexts(ThemePreset theme)
        {
            foreach (var text in FindObjectsOfType<TextMeshProUGUI>(true))
            {
                ThemeableUI themeable = text.GetComponent<ThemeableUI>();
                if (themeable == null)
                    themeable = text.gameObject.AddComponent<ThemeableUI>();

                themeable.elementType = ResolveTextElementType(text);
                themeable.ApplyTheme();
            }
        }

        private UIElementType ResolveImageElementType(Image image)
        {
            string name = image.gameObject.name.ToLower();

            if (image.GetComponent<Button>() != null)
            {
                return name.Contains("secondary")
                    ? UIElementType.SecondaryButton
                    : UIElementType.PrimaryButton;
            }

            if (image.GetComponentInParent<TMP_InputField>() != null)
            {
                return UIElementType.InputField;
            }

            if (name.Contains("card") || name.Contains("panel"))
                return UIElementType.CardBackground;

            if (name.Contains("background") || name.Contains("screen"))
                return UIElementType.ScreenBackground;

            return UIElementType.CardBackground;
        }

        private UIElementType ResolveTextElementType(TextMeshProUGUI text)
        {
            string name = text.gameObject.name.ToLower();

            if (name.Contains("error"))
                return UIElementType.ErrorText;

            if (text.GetComponentInParent<Button>() != null)
                return UIElementType.ButtonText;

            if (name.Contains("title") || name.Contains("heading") || text.fontSize >= 30)
                return UIElementType.HeadingText;

            if (name.Contains("subtitle") || text.fontSize >= 20)
                return UIElementType.SubheadingText;

            if (name.Contains("placeholder"))
                return UIElementType.PlaceholderText;

            if (name.Contains("label"))
                return UIElementType.LabelText;

            return UIElementType.BodyText;
        }
    }
}
