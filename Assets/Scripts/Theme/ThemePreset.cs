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
        public Color primaryMedicalBlue = new Color32(44, 125, 160, 255); // #2C7DA0
        public Color primaryDarkBlue = new Color32(26, 54, 93, 255);      // #1A365D
        public Color backgroundLight = new Color32(245, 247, 250, 255);   // #F5F7FA
        public Color cardWhite = new Color32(255, 255, 255, 255);         // #FFFFFF
        public Color textDark = new Color32(30, 41, 59, 255);             // #1E293B
        public Color textLight = new Color32(100, 116, 139, 255);         // #64748B
        public Color errorRed = new Color32(229, 62, 62, 255);            // #E53E3E
        public Color successGreen = new Color32(56, 161, 105, 255);       // #38A169
        public Color borderGray = new Color32(226, 232, 240, 255);        // #E2E8F0

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
        public Vector2 shadowOffset = new Vector2(0, -4f);
        public float shadowOpacity = 0.1f;
        public float inputFieldHeight = 50f;
        public float buttonHeight = 55f;
    }
}
