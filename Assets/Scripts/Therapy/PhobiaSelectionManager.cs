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

        private GameObject levelPanel;

        private void Start()
        {
            AutoBindMissingFields();

            if (heightPhobiaButton != null)
            {
                heightPhobiaButton.onClick.RemoveAllListeners();
                heightPhobiaButton.onClick.AddListener(() => SelectPhobia("Height"));
            }
                
            if (darknessPhobiaButton != null)
            {
                darknessPhobiaButton.onClick.RemoveAllListeners();
                darknessPhobiaButton.onClick.AddListener(() => SelectPhobia("Darkness"));
            }
                
            if (crowdPhobiaButton != null)
            {
                crowdPhobiaButton.onClick.RemoveAllListeners();
                crowdPhobiaButton.onClick.AddListener(() => SelectPhobia("Crowd"));
            }

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
            
            // Phase 2: show difficulty selection (SRS §3.2.2, §3.2.7).
            ShowLevelSelection();
        }

        private void ShowLevelSelection()
        {
            if (heightPhobiaButton != null) heightPhobiaButton.gameObject.SetActive(false);
            if (darknessPhobiaButton != null) darknessPhobiaButton.gameObject.SetActive(false);
            if (crowdPhobiaButton != null) crowdPhobiaButton.gameObject.SetActive(false);

            if (instructionText != null)
                instructionText.text = $"Select the exposure level for {UserData.SelectedPhobia} Phobia:";

            levelPanel = new GameObject("LevelSelectionPanel", typeof(RectTransform), typeof(VerticalLayoutGroup));
            levelPanel.transform.SetParent(heightPhobiaButton.transform.parent, false);

            var layout = levelPanel.GetComponent<VerticalLayoutGroup>();
            var parentLayout = heightPhobiaButton.transform.parent.GetComponent<VerticalLayoutGroup>();
            if (parentLayout != null)
            {
                layout.spacing = parentLayout.spacing;
                layout.childAlignment = parentLayout.childAlignment;
                layout.childControlWidth = parentLayout.childControlWidth;
                layout.childControlHeight = parentLayout.childControlHeight;
                layout.childForceExpandWidth = parentLayout.childForceExpandWidth;
                layout.childForceExpandHeight = parentLayout.childForceExpandHeight;
            }

            CreateLevelButton("Level 1: Beginner (Low)", () => ConfirmLevel("Low", 1));
            CreateLevelButton("Level 2: Intermediate (Medium)", () => ConfirmLevel("Medium", 2));
            CreateLevelButton("Level 3: Advanced (High)", () => ConfirmLevel("High", 3));
            CreateLevelButton("Back to Phobias", CancelLevelSelection, true);
        }

        private void CreateLevelButton(string label, System.Action onClickAction, bool isBackButton = false)
        {
            if (heightPhobiaButton == null) return;

            GameObject btnGO = Instantiate(heightPhobiaButton.gameObject, levelPanel.transform);
            btnGO.SetActive(true);
            btnGO.name = label.Replace(" ", "");

            Button btn = btnGO.GetComponent<Button>();
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => onClickAction());

            if (isBackButton)
            {
                var img = btn.GetComponent<Image>();
                if (img != null)
                {
                    img.color = new Color32(45, 45, 60, 255);
                }
            }

            var text = btnGO.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null)
            {
                text.text = label;
            }
            else
            {
                var legacyText = btnGO.GetComponentInChildren<Text>();
                if (legacyText != null)
                    legacyText.text = label;
            }
        }

        private void ConfirmLevel(string levelName, int stage)
        {
            UserData.SelectedDifficulty = levelName;
            UserData.CurrentStage = stage;
            Debug.Log($"Level Confirmed: {levelName} (Stage {stage})");

            if (levelPanel != null)
                Destroy(levelPanel);

            SceneLoader.Instance.LoadScene("BaselineScene");
        }

        private void CancelLevelSelection()
        {
            if (levelPanel != null)
                Destroy(levelPanel);

            if (instructionText != null)
                instructionText.text = "Choose a phobia track to begin your exposure therapy.";

            if (heightPhobiaButton != null) heightPhobiaButton.gameObject.SetActive(true);
            if (darknessPhobiaButton != null) darknessPhobiaButton.gameObject.SetActive(true);
            if (crowdPhobiaButton != null) crowdPhobiaButton.gameObject.SetActive(true);
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
