using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

namespace PhobiaReliefTherapy.VR
{
    /// <summary>
    /// Attach to any TMP_InputField or legacy InputField in VR.
    /// On click/select, summons the world-space VRKeyboard and routes key presses back.
    /// </summary>
    public class VRKeyboardTrigger : MonoBehaviour, IPointerDownHandler, ISelectHandler
    {
        private TMP_InputField  tmpField;
        private InputField      legacyField;

        private void Awake()
        {
            tmpField    = GetComponent<TMP_InputField>();
            legacyField = GetComponent<InputField>();

            if (tmpField != null)
            {
                tmpField.shouldHideMobileInput = true;
                tmpField.shouldHideSoftKeyboard = true;
            }
            if (legacyField != null)
                legacyField.shouldHideMobileInput = true;
        }

        public void OnPointerDown(PointerEventData eventData) => OpenKeyboard();
        public void OnSelect(BaseEventData eventData)         => OpenKeyboard();

        private void OpenKeyboard()
        {
            if (tmpField != null)
            {
                tmpField.shouldHideMobileInput = true;
                tmpField.shouldHideSoftKeyboard = true;
            }

            VRKeyboard kb = VRKeyboard.EnsureInstance();
            if (tmpField    != null) { kb.Open(tmpField);    return; }
            if (legacyField != null) { kb.Open(legacyField); return; }
        }
    }
}
