using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PhobiaReliefTherapy.Data;
using PhobiaReliefTherapy.Managers;

namespace PhobiaReliefTherapy.Therapy
{
    /// <summary>
    /// Handles the selection of the phobia type from the Dashboard/Selection Scene.
    /// </summary>
    public class PhobiaSelectionManager : MonoBehaviour
    {
        [Header("Selection Buttons")]
        public Button heightPhobiaButton;
        public Button darknessPhobiaButton;
        public Button crowdPhobiaButton;

        [Header("UI Feedback")]
        public TextMeshProUGUI instructionText;
        public TextMeshProUGUI selectedPhobiaText;

        private void Start()
        {
            AutoBindMissingFields();

            if (heightPhobiaButton != null)
                heightPhobiaButton.onClick.AddListener(() => SelectPhobia("Height"));
                
            if (darknessPhobiaButton != null)
                darknessPhobiaButton.onClick.AddListener(() => SelectPhobia("Darkness"));
                
            if (crowdPhobiaButton != null)
                crowdPhobiaButton.onClick.AddListener(() => SelectPhobia("Crowd"));

            if (instructionText != null)
                instructionText.text = "Choose a phobia track to begin your exposure therapy.";

            UpdateSelectedPhobiaText();
        }

        private void AutoBindMissingFields()
        {
            if (heightPhobiaButton == null)
                heightPhobiaButton = AutoBindField<Button>("HeightButton");
            if (darknessPhobiaButton == null)
                darknessPhobiaButton = AutoBindField<Button>("DarknessButton");
            if (crowdPhobiaButton == null)
                crowdPhobiaButton = AutoBindField<Button>("CrowdButton");
            if (instructionText == null)
                instructionText = AutoBindField<TextMeshProUGUI>("InstructionText");
            if (selectedPhobiaText == null)
                selectedPhobiaText = AutoBindField<TextMeshProUGUI>("SelectedPhobiaText");
        }

        private T AutoBindField<T>(string objectName) where T : Component
        {
            T result = AutoBindHelper.FindComponentInChildrenByName<T>(transform, objectName);
            return result != null ? result : AutoBindHelper.FindComponentByName<T>(objectName);
        }

        private void SelectPhobia(string phobiaType)
        {
            Debug.Log($"Phobia Selected: {phobiaType}");
            UserData.SelectedPhobia = phobiaType;
            UpdateSelectedPhobiaText();
            
            // After selecting, move to Baseline measurement
            SceneLoader.Instance.LoadScene("BaselineScene");
        }

        private void UpdateSelectedPhobiaText()
        {
            if (selectedPhobiaText != null)
            {
                selectedPhobiaText.text = UserData.SelectedPhobia == "None"
                    ? "No phobia selected yet."
                    : $"Selected: {UserData.SelectedPhobia} Therapy";
            }
        }
    }
}
