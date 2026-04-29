using UnityEngine;

namespace PhobiaReliefTherapy.Theme
{
    /// <summary>
    /// Singleton manager that holds the active theme. 
    /// Marked as ExecuteAlways so UI elements can preview changes in Editor without pressing Play.
    /// </summary>
    [ExecuteAlways]
    public class ThemeManager : MonoBehaviour
    {
        public static ThemeManager Instance { get; private set; }

        [Tooltip("The active UI theme. Swap this to instantly change the app's look.")]
        public ThemePreset currentTheme;

        private void Awake()
        {
            if (Instance == null) Instance = this;
        }

        private void OnEnable()
        {
            if (Instance == null) Instance = this;
        }

        private void OnValidate()
        {
            // If the user swaps the Theme Preset in the inspector, notify all ThemeableUI components
            if (currentTheme != null)
            {
                ThemeableUI[] allUI = FindObjectsOfType<ThemeableUI>();
                foreach (var ui in allUI)
                {
                    ui.ApplyTheme();
                }
            }
        }
    }
}
