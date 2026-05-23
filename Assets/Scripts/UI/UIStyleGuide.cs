using UnityEngine;
using UnityEngine.UI;

namespace PhobiaReliefTherapy.UI
{
    public enum UITextStyle
    {
        Heading,
        Body,
        Button,
        Placeholder,
        Caption
    }

    [CreateAssetMenu(fileName = "UIStyleGuide", menuName = "PhobiaReliefTherapy/UI Style Guide")]
    public class UIStyleGuide : ScriptableObject
    {
        [Header("Colors")]
        public Color backgroundColor = new Color32(13, 14, 18, 255); // #0D0E12
        public Color cardColor = new Color32(31, 34, 42, 255); // #1F222A
        public Color inputBackgroundColor = new Color32(19, 21, 26, 255); // #13151A
        public Color primaryButtonColor = new Color32(255, 87, 34, 255); // #FF5722
        public Color primaryButtonHighlightColor = new Color32(255, 112, 67, 255); // #FF7043
        public Color primaryButtonPressedColor = new Color32(230, 74, 25, 255); // #E64A19
        public Color textPrimaryColor = Color.white;
        public Color textSecondaryColor = new Color32(220, 225, 235, 255); // Crisp light grey/white
        public Color placeholderColor = new Color32(140, 144, 153, 200);
        public Color inputSelectionColor = new Color32(255, 87, 34, 128);
        public Color cardShadowColor = new Color32(0, 0, 0, 150);

        [Header("Gradients")]
        public bool useGradients = true;
        public Color screenBackgroundGradientTop = new Color32(32, 35, 42, 255); // #20232A
        public Color screenBackgroundGradientBottom = new Color32(11, 12, 15, 255); // #0B0C0F
        public Color cardBackgroundGradientTop = new Color32(37, 41, 51, 255); // #252933
        public Color cardBackgroundGradientBottom = new Color32(23, 25, 32, 255); // #171920
        public Color primaryButtonGradientTop = new Color32(255, 87, 34, 255); // #FF5722 (Orange)
        public Color primaryButtonGradientBottom = new Color32(216, 67, 21, 255); // #D84315 (Copper)
        public Color inputFieldGradientTop = new Color32(24, 26, 33, 255); // #181A21
        public Color inputFieldGradientBottom = new Color32(14, 16, 20, 255); // #0E1014

        [Header("Typography")]
        public Font modernFont;
        public int headingFontSize = 32;
        public int bodyFontSize = 16;
        public int buttonFontSize = 18;
        public int inputFontSize = 16;
        public FontStyle headingFontStyle = FontStyle.Bold;
        public FontStyle bodyFontStyle = FontStyle.Normal;
        public FontStyle buttonFontStyle = FontStyle.Bold;
        public FontStyle inputFontStyle = FontStyle.Normal;

        [Header("Layout")]
        public float cardPadding = 24f;
        public float elementSpacing = 16f;
        public float buttonWidth = 220f;
        public float buttonHeight = 50f;
        public float buttonCornerRadius = 12f;
        public float inputHeight = 48f;
        public float inputCornerRadius = 8f;
        public float cardCornerRadius = 24f;
        public float cardShadowDistance = 8f;
        public Sprite roundedSprite;

        public void ApplyToText(Text text, UITextStyle style)
        {
            if (text == null)
                return;

            if (modernFont != null)
                text.font = modernFont;

            switch (style)
            {
                case UITextStyle.Heading:
                    text.fontSize = headingFontSize;
                    text.fontStyle = headingFontStyle;
                    text.color = textPrimaryColor;
                    break;
                case UITextStyle.Body:
                    text.fontSize = bodyFontSize;
                    text.fontStyle = bodyFontStyle;
                    text.color = textSecondaryColor;
                    break;
                case UITextStyle.Button:
                    text.fontSize = buttonFontSize;
                    text.fontStyle = buttonFontStyle;
                    text.color = textPrimaryColor;
                    break;
                case UITextStyle.Placeholder:
                    text.fontSize = inputFontSize;
                    text.fontStyle = inputFontStyle;
                    text.color = placeholderColor;
                    break;
                case UITextStyle.Caption:
                    text.fontSize = bodyFontSize;
                    text.fontStyle = bodyFontStyle;
                    text.color = textSecondaryColor;
                    break;
            }
        }

        public void ApplyToButton(Button button, Image targetImage)
        {
            if (button == null || targetImage == null)
                return;

            if (useGradients)
            {
                targetImage.color = Color.white;
                var grad = targetImage.GetComponent<UIGradient>();
                if (grad == null) grad = targetImage.gameObject.AddComponent<UIGradient>();
                grad.topColor = primaryButtonGradientTop;
                grad.bottomColor = primaryButtonGradientBottom;
                grad.vertical = true;
                targetImage.SetVerticesDirty();
            }
            else
            {
                var grad = targetImage.GetComponent<UIGradient>();
                if (grad != null) DestroyImmediate(grad);
                targetImage.color = primaryButtonColor;
            }

            var colors = button.colors;
            colors.normalColor = useGradients ? Color.white : primaryButtonColor;
            colors.highlightedColor = useGradients ? new Color(1f, 1f, 1f, 0.9f) : primaryButtonHighlightColor;
            colors.pressedColor = useGradients ? new Color(0.8f, 0.8f, 0.8f, 1f) : primaryButtonPressedColor;
            colors.disabledColor = new Color32(70, 70, 95, 255);
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.1f;
            button.colors = colors;

            if (button.transition != Selectable.Transition.ColorTint)
                button.transition = Selectable.Transition.ColorTint;

            targetImage.sprite = GetRoundedSprite(targetImage.sprite);
            if (targetImage.type != Image.Type.Sliced)
                targetImage.type = Image.Type.Sliced;
        }

        public Sprite GetRoundedSprite(Sprite defaultSprite)
        {
            if (roundedSprite != null)
                return roundedSprite;
            if (defaultSprite != null)
                return defaultSprite;

            // Fallback: use a simple white texture sprite to avoid null images.
            var whiteTex = Texture2D.whiteTexture;
            if (whiteTex != null)
            {
                return Sprite.Create(whiteTex, new Rect(0, 0, whiteTex.width, whiteTex.height), new Vector2(0.5f, 0.5f));
            }

            return null;
        }
    }
}
