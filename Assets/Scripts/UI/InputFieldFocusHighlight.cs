using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace PhobiaReliefTherapy.UI
{
    /// <summary>
    /// Highlights the border outline of an InputField or TMP_InputField when it is focused (selected).
    /// </summary>
    [RequireComponent(typeof(Outline))]
    public class InputFieldFocusHighlight : MonoBehaviour, ISelectHandler, IDeselectHandler
    {
        private Outline outline;
        
        [Tooltip("Outline color when input field is not focused")]
        public Color normalColor = new Color(0, 0, 0, 0);

        [Tooltip("Outline color when input field is focused (same as accent color)")]
        public Color focusColor = new Color32(74, 111, 255, 255);

        [Tooltip("Outline thickness/distance when focused")]
        public Vector2 focusDistance = new Vector2(2f, -2f);

        private void Awake()
        {
            outline = GetComponent<Outline>();
            if (outline == null)
            {
                outline = gameObject.AddComponent<Outline>();
            }
            // Start in normal (hidden) state
            outline.effectColor = normalColor;
            outline.effectDistance = Vector2.zero;
        }

        private void OnEnable()
        {
            // Reset to normal state on enable
            if (outline != null)
            {
                outline.effectColor = normalColor;
                outline.effectDistance = Vector2.zero;
            }
        }

        public void OnSelect(BaseEventData eventData)
        {
            if (outline != null)
            {
                outline.effectColor = focusColor;
                outline.effectDistance = focusDistance;
            }
        }

        public void OnDeselect(BaseEventData eventData)
        {
            if (outline != null)
            {
                outline.effectColor = normalColor;
                outline.effectDistance = Vector2.zero;
            }
        }
    }
}
