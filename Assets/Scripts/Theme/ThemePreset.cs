using UnityEngine;
using TMPro;

namespace PhobiaReliefTherapy.Theme
{
    /// <summary>
    /// A centralized repository for all aesthetic values. 
    /// Ensures a medical-grade, sharp, professional UI without hardcoded guesswork.
    /// </summary>
    [CreateAssetMenu(fileName = "NewThemePreset", menuName = "Therapy UI/Theme Preset")]
    public class ThemePreset : ScriptableObject
    {
        [Header("Colors - Medical Grade Solid")]
        public Color primaryMedicalBlue = new Color32(255, 87, 34, 255); // #FF5722 (Orange Accent)
        public Color primaryButtonHighlight = new Color32(255, 112, 67, 255); // #FF7043
        public Color primaryButtonPressed = new Color32(230, 74, 25, 255); // #E64A19
        public Color primaryDarkBlue = new Color32(255, 255, 255, 255); // White for clean titles
        public Color backgroundLight = new Color32(13, 14, 18, 255); // #0D0E12 (Charcoal)
        public Color cardWhite = new Color32(31, 34, 42, 255); // #1F222A (Card Dark)
        public Color textDark = new Color32(255, 255, 255, 255); // White primary text
        public Color textLight = new Color32(220, 225, 235, 255); // Crisp light grey/white
        public Color errorRed = new Color32(229, 62, 62, 255);
        public Color successGreen = new Color32(56, 161, 105, 255);
        public Color borderGray = new Color32(44, 48, 59, 255); // #2C303B
        public Color inputFieldColor = new Color32(19, 21, 26, 255); // #13151A

        [Header("Gradient Colors")]
        public bool useGradients = true;
        public Color screenBackgroundGradientTop = new Color32(32, 35, 42, 255); // #20232A
        public Color screenBackgroundGradientBottom = new Color32(11, 12, 15, 255); // #0B0C0F
        public Color cardBackgroundGradientTop = new Color32(37, 41, 51, 255); // #252933
        public Color cardBackgroundGradientBottom = new Color32(23, 25, 32, 255); // #171920
        public Color primaryButtonGradientTop = new Color32(255, 87, 34, 255); // #FF5722 (Orange)
        public Color primaryButtonGradientBottom = new Color32(216, 67, 21, 255); // #D84315 (Copper)
        public Color secondaryButtonGradientTop = new Color32(47, 51, 62, 255); // #2F333E
        public Color secondaryButtonGradientBottom = new Color32(31, 34, 43, 255); // #1F222B
        public Color inputFieldGradientTop = new Color32(24, 26, 33, 255); // #181A21
        public Color inputFieldGradientBottom = new Color32(14, 16, 20, 255); // #0E1014

        [Header("Typography - TextMeshPro")]
        [Tooltip("The crisp TextMeshPro font to enforce globally")]
        public TMP_FontAsset globalFont;
        
        public int headingFontSize = 34;
        public int subheadingFontSize = 22;
        public int bodyFontSize = 18;
        public int labelFontSize = 14;
        public int buttonFontSize = 20;

        [Header("Spacing & Sizing")]
        public float standardPadding = 20f;
        public float cornerRadius = 12f; // Unity's default Background sprite approximates this perfectly
        public Sprite roundedSprite;
        public Vector2 shadowOffset = new Vector2(0, -4f);
        public float shadowOpacity = 0.1f;
        public float inputFieldHeight = 48f;
        public float buttonHeight = 55f;
    }
}
