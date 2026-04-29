using UnityEngine;
using UnityEngine.EventSystems;

namespace PhobiaReliefTherapy.UI
{
    /// <summary>
    /// Adds a modern, smooth scaling animation to buttons when hovered.
    /// This makes the UI feel alive and premium, like a high-end application.
    /// </summary>
    public class ButtonHoverEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private Vector3 originalScale;
        private bool isHovered = false;
        
        [Tooltip("How much the button scales up when hovered")]
        public float scaleMultiplier = 1.05f;

        private void Start()
        {
            originalScale = transform.localScale;
        }

        private void Update()
        {
            // Smoothly animate towards the target scale using Lerp
            Vector3 targetScale = isHovered ? originalScale * scaleMultiplier : originalScale;
            transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * 15f);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            isHovered = true;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            isHovered = false;
        }
    }
}
