using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace PhobiaReliefTherapy.Theme
{
    public enum UIElementType
    {
        ScreenBackground,
        CardBackground,
        PrimaryButton,
        SecondaryButton,
        HeadingText,
        SubheadingText,
        BodyText,
        LabelText,
        ErrorText,
        ButtonText,
        InputField
    }

    /// <summary>
    /// Base script attached to any UI element. It automatically fetches the global theme 
    /// and applies strict solid colors, typography, and spacing to guarantee a sharp medical look.
    /// </summary>
    [ExecuteAlways]
    public class ThemeableUI : MonoBehaviour
    {
        public UIElementType elementType;
        [Tooltip("Check this if you want to use a custom font size here instead of the global theme's size")]
        public bool overrideTypography = false;

        private void OnEnable()
        {
            ApplyTheme();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Instantly apply theme when modifying this component in the Inspector
            UnityEditor.EditorApplication.delayCall += () => { if (this != null) ApplyTheme(); };
        }
#endif

        public void ApplyTheme()
        {
            // NEW TRULY GLOBAL SYSTEM: Loads the theme directly from the Resources folder!
            // This means it works instantly in EVERY scene without needing a ThemeManager GameObject.
            ThemePreset theme = Resources.Load<ThemePreset>("MedicalTheme");
            
            if (theme == null) 
            {
                // Fallback check if they haven't moved it yet
                if (ThemeManager.Instance != null && ThemeManager.Instance.currentTheme != null)
                    theme = ThemeManager.Instance.currentTheme;
                else
                    return;
            }

            // Enforce sharp, non-blurry, medical aesthetic
            switch (elementType)
            {
                case UIElementType.ScreenBackground:
                    ApplyImageStyle(theme.backgroundLight, false, theme);
                    break;
                case UIElementType.CardBackground:
                    ApplyImageStyle(theme.cardWhite, true, theme);
                    break;
                case UIElementType.PrimaryButton:
                    ApplyImageStyle(theme.primaryMedicalBlue, true, theme);
                    ApplyRectSize(theme.buttonHeight);
                    break;
                case UIElementType.SecondaryButton:
                    ApplyImageStyle(theme.borderGray, true, theme);
                    ApplyRectSize(theme.buttonHeight);
                    break;
                case UIElementType.InputField:
                    ApplyImageStyle(theme.cardWhite, true, theme);
                    ApplyRectSize(theme.inputFieldHeight);
                    break;
                case UIElementType.HeadingText:
                    ApplyTextStyle(theme.primaryDarkBlue, theme.headingFontSize, FontStyles.Bold, theme);
                    break;
                case UIElementType.SubheadingText:
                    ApplyTextStyle(theme.primaryMedicalBlue, theme.subheadingFontSize, FontStyles.Normal, theme);
                    break;
                case UIElementType.BodyText:
                    ApplyTextStyle(theme.textDark, theme.bodyFontSize, FontStyles.Normal, theme);
                    break;
                case UIElementType.LabelText:
                    ApplyTextStyle(theme.textLight, theme.labelFontSize, FontStyles.Bold | FontStyles.UpperCase, theme);
                    break;
                case UIElementType.ErrorText:
                    ApplyTextStyle(theme.errorRed, theme.bodyFontSize, FontStyles.Bold, theme);
                    break;
                case UIElementType.ButtonText:
                    ApplyTextStyle(theme.cardWhite, theme.buttonFontSize, FontStyles.Bold, theme);
                    break;
            }
        }

        private void ApplyImageStyle(Color color, bool addShadow, ThemePreset theme)
        {
            Image img = GetComponent<Image>();
            if (img != null)
            {
                img.color = color;
                
                // NO GRADIENTS ALLOWED IN MEDICAL UI
                var grad = GetComponent("UIGradient");
                if (grad != null) DestroyImmediate(grad);
            }

            if (addShadow)
            {
                Shadow shadow = GetComponent<Shadow>();
                if (shadow == null) shadow = gameObject.AddComponent<Shadow>();
                shadow.effectColor = new Color(0, 0, 0, theme.shadowOpacity);
                shadow.effectDistance = theme.shadowOffset;
            }
        }

        private void ApplyTextStyle(Color color, int fontSize, FontStyles style, ThemePreset theme)
        {
            TextMeshProUGUI txt = GetComponent<TextMeshProUGUI>();
            if (txt != null)
            {
                txt.color = color;
                
                if (!overrideTypography)
                {
                    txt.fontSize = fontSize;
                    txt.fontStyle = style;
                    if (theme.globalFont != null) txt.font = theme.globalFont;
                }
            }
        }

        private void ApplyRectSize(float height)
        {
            RectTransform rect = GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.sizeDelta = new Vector2(rect.sizeDelta.x, height);
            }
        }
    }
}
