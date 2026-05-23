using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PhobiaReliefTherapy.UI;

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
        InputField,
        PlaceholderText
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
                    ApplyUIStyle(theme.backgroundLight, theme.screenBackgroundGradientTop, theme.screenBackgroundGradientBottom, false, theme);
                    break;
                case UIElementType.CardBackground:
                    ApplyUIStyle(theme.cardWhite, theme.cardBackgroundGradientTop, theme.cardBackgroundGradientBottom, true, theme);
                    break;
                case UIElementType.PrimaryButton:
                    ApplyUIStyle(theme.primaryMedicalBlue, theme.primaryButtonGradientTop, theme.primaryButtonGradientBottom, true, theme);
                    ApplyRectSize(theme.buttonHeight);
                    ApplyButtonStyle(GetComponent<Button>(), theme.primaryMedicalBlue, theme.primaryButtonHighlight, theme.primaryButtonPressed, theme);
                    if (GetComponent<ButtonHoverEffect>() == null)
                    {
                        gameObject.AddComponent<ButtonHoverEffect>();
                    }
                    break;
                case UIElementType.SecondaryButton:
                    ApplyUIStyle(theme.borderGray, theme.secondaryButtonGradientTop, theme.secondaryButtonGradientBottom, true, theme);
                    ApplyRectSize(theme.buttonHeight);
                    ApplyButtonStyle(GetComponent<Button>(), theme.borderGray, new Color(theme.borderGray.r * 1.2f, theme.borderGray.g * 1.2f, theme.borderGray.b * 1.2f), new Color(theme.borderGray.r * 0.8f, theme.borderGray.g * 0.8f, theme.borderGray.b * 0.8f), theme);
                    if (GetComponent<ButtonHoverEffect>() == null)
                    {
                        gameObject.AddComponent<ButtonHoverEffect>();
                    }
                    break;
                case UIElementType.InputField:
                    ApplyUIStyle(theme.inputFieldColor, theme.inputFieldGradientTop, theme.inputFieldGradientBottom, true, theme);
                    ApplyRectSize(theme.inputFieldHeight);
                    var focusHighlight = GetComponent<InputFieldFocusHighlight>();
                    if (focusHighlight == null)
                    {
                        focusHighlight = gameObject.AddComponent<InputFieldFocusHighlight>();
                    }
                    focusHighlight.focusColor = theme.primaryMedicalBlue;
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
                    ApplyTextStyle(Color.white, theme.buttonFontSize, FontStyles.Bold, theme);
                    break;
                case UIElementType.PlaceholderText:
                    ApplyTextStyle(theme.textLight, theme.bodyFontSize, FontStyles.Normal, theme);
                    break;
            }
        }

        private void ApplyUIStyle(Color solidColor, Color gradTop, Color gradBottom, bool addShadow, ThemePreset theme)
        {
            Image img = GetComponent<Image>();
            if (img != null)
            {
                if (theme.roundedSprite != null && elementType != UIElementType.ScreenBackground)
                {
                    img.sprite = theme.roundedSprite;
                    img.type = Image.Type.Sliced;
                }

                if (theme.useGradients)
                {
                    ApplyGradient(gradTop, gradBottom);
                }
                else
                {
                    RemoveGradient();
                    img.color = solidColor;
                }
            }

            if (addShadow)
            {
                Shadow shadow = GetComponent<Shadow>();
                if (shadow == null) shadow = gameObject.AddComponent<Shadow>();
                shadow.effectColor = new Color(0, 0, 0, theme.shadowOpacity);
                shadow.effectDistance = theme.shadowOffset;
            }
        }

        private void ApplyGradient(Color top, Color bottom, bool vertical = true)
        {
            Image img = GetComponent<Image>();
            if (img == null) return;

            img.color = Color.white;

            var grad = GetComponent<UIGradient>();
            if (grad == null) grad = gameObject.AddComponent<UIGradient>();

            grad.topColor = top;
            grad.bottomColor = bottom;
            grad.vertical = vertical;

            img.SetVerticesDirty();
        }

        private void RemoveGradient()
        {
            var grad = GetComponent<UIGradient>();
            if (grad != null)
            {
                if (Application.isPlaying)
                    Destroy(grad);
                else
                    DestroyImmediate(grad);
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

        private void ApplyButtonStyle(Button button, Color normalColor, Color highlightColor, Color pressedColor, ThemePreset theme)
        {
            if (button == null) return;
            
            button.transition = Selectable.Transition.ColorTint;
            var colors = button.colors;
            colors.normalColor = theme.useGradients ? Color.white : normalColor;
            colors.highlightedColor = theme.useGradients ? new Color(1f, 1f, 1f, 0.9f) : highlightColor;
            colors.pressedColor = theme.useGradients ? new Color(0.8f, 0.8f, 0.8f, 1f) : pressedColor;
            colors.disabledColor = new Color32(70, 70, 95, 255);
            colors.fadeDuration = 0.1f;
            button.colors = colors;
        }
    }
}
