using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using PhobiaReliefTherapy.Data;
using PhobiaReliefTherapy.Managers;
using PhobiaReliefTherapy.UI;
using PhobiaReliefTherapy.Theme;

namespace PhobiaReliefTherapy.Therapy
{
    /// <summary>
    /// UC-03 through UC-06: browse phobia modules, descriptions, difficulty, then baseline.
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

        private GameObject detailRoot;
        private GameObject browseRoot;
        private string pendingPhobia;

        private void Start()
        {
            AutoBindMissingFields();
            if (SceneManager.GetActiveScene().name == "PhobiaSelectionScene")
                EnsureSelectionUi();
            WirePhobiaButton(heightPhobiaButton, "Height");
            WirePhobiaButton(darknessPhobiaButton, "Darkness");
            WirePhobiaButton(crowdPhobiaButton, "Crowd");

            if (instructionText != null)
                instructionText.text = "Choose a phobia track. AI will set the starting stage from baseline heart rate and head movement.";

            UpdateSelectedPhobiaText();
        }

        private void WirePhobiaButton(Button button, string phobia)
        {
            if (button == null)
                return;

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => ShowPhobiaDetail(phobia));
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

        private void EnsureSelectionUi()
        {
            if (heightPhobiaButton != null && darknessPhobiaButton != null && crowdPhobiaButton != null)
                return;

            var canvas = ClinicalUIFactory.EnsureOverlayCanvas();
            var card = ClinicalUIFactory.CreateCard(ClinicalUIFactory.OverlayRoot(canvas), new Vector2(920, 680), "PhobiaSelectCard");

            instructionText = ClinicalUIFactory.CreateText("InstructionText", card.transform,
                "Choose a phobia track to begin exposure therapy.", 22, TextAlignmentOptions.Center, UIElementType.HeadingText);
            ClinicalUIFactory.Anchor(instructionText.rectTransform, new Vector2(0.08f, 0.82f), new Vector2(0.92f, 0.94f));

            heightPhobiaButton = ClinicalUIFactory.CreateButton("HeightButton", card.transform, "Height Phobia");
            ClinicalUIFactory.Anchor(heightPhobiaButton.GetComponent<RectTransform>(), new Vector2(0.18f, 0.62f), new Vector2(0.82f, 0.62f), new Vector2(0, 58));

            darknessPhobiaButton = ClinicalUIFactory.CreateButton("DarknessButton", card.transform, "Darkness Phobia");
            ClinicalUIFactory.Anchor(darknessPhobiaButton.GetComponent<RectTransform>(), new Vector2(0.18f, 0.46f), new Vector2(0.82f, 0.46f), new Vector2(0, 58));

            crowdPhobiaButton = ClinicalUIFactory.CreateButton("CrowdButton", card.transform, "Crowd Phobia");
            ClinicalUIFactory.Anchor(crowdPhobiaButton.GetComponent<RectTransform>(), new Vector2(0.18f, 0.30f), new Vector2(0.82f, 0.30f), new Vector2(0, 58));

            var back = ClinicalUIFactory.CreateButton("BackDashButton", card.transform, "Back to dashboard", false);
            ClinicalUIFactory.Anchor(back.GetComponent<RectTransform>(), new Vector2(0.32f, 0.12f), new Vector2(0.68f, 0.12f), new Vector2(0, 48));
            back.onClick.AddListener(() => SceneLoader.Instance.LoadScene("DashboardScene"));
            browseRoot = card;
        }

        private void ShowPhobiaDetail(string phobiaType)
        {
            pendingPhobia = phobiaType;
            UserData.SelectedPhobia = PhobiaCatalog.Normalize(phobiaType);
            UpdateSelectedPhobiaText();
            CacheBrowseRoot();
            SetBrowseVisible(false);

            if (detailRoot != null)
                Destroy(detailRoot);

            var canvas = ClinicalUIFactory.EnsureOverlayCanvas();
            detailRoot = ClinicalUIFactory.CreateFullScreenPanel(canvas.transform, "PhobiaDetailOverlay");
            var card = ClinicalUIFactory.CreateCard(detailRoot.transform, new Vector2(860, 700), "PhobiaDetailCard");

            var title = ClinicalUIFactory.CreateText("DetailTitle", card.transform, phobiaType + " Phobia", 32, TextAlignmentOptions.Center, UIElementType.HeadingText);
            ClinicalUIFactory.Anchor(title.rectTransform, new Vector2(0.08f, 0.86f), new Vector2(0.92f, 0.96f));

            var body = ClinicalUIFactory.CreateText("DetailBody", card.transform,
                PhobiaCatalog.RecommendedStart(phobiaType) + "\n\n" +
                PhobiaCatalog.PreSessionInstructions(),
                18, TextAlignmentOptions.TopLeft);
            ClinicalUIFactory.Anchor(body.rectTransform, new Vector2(0.08f, 0.42f), new Vector2(0.92f, 0.84f));

            var beginner = ClinicalUIFactory.CreateButton("BeginnerBtn", card.transform, "Beginner (Stage 1)");
            ClinicalUIFactory.Anchor(beginner.GetComponent<RectTransform>(), new Vector2(0.18f, 0.28f), new Vector2(0.37f, 0.28f), new Vector2(0, 52));
            beginner.onClick.AddListener(() => ConfirmSelection("Low"));

            var intermediate = ClinicalUIFactory.CreateButton("IntermediateBtn", card.transform, "Intermediate (Stage 2)", false);
            ClinicalUIFactory.Anchor(intermediate.GetComponent<RectTransform>(), new Vector2(0.405f, 0.28f), new Vector2(0.595f, 0.28f), new Vector2(0, 52));
            intermediate.onClick.AddListener(() => ConfirmSelection("Medium"));

            var advanced = ClinicalUIFactory.CreateButton("AdvancedBtn", card.transform, "Advanced (Stage 3)", false);
            ClinicalUIFactory.Anchor(advanced.GetComponent<RectTransform>(), new Vector2(0.63f, 0.28f), new Vector2(0.82f, 0.28f), new Vector2(0, 52));
            advanced.onClick.AddListener(() => ConfirmSelection("High"));

            bool showManualStage = UserData.AllowManualStageSelection;
            beginner.gameObject.SetActive(showManualStage);
            intermediate.gameObject.SetActive(showManualStage);
            advanced.gameObject.SetActive(showManualStage);

            var start = ClinicalUIFactory.CreateButton("StartBaselineBtn", card.transform, "Continue to baseline");
            ClinicalUIFactory.Anchor(start.GetComponent<RectTransform>(), new Vector2(0.18f, 0.26f), new Vector2(0.82f, 0.26f), new Vector2(0, 54));
            start.onClick.AddListener(() => ConfirmSelection("High"));
            start.gameObject.SetActive(!showManualStage);

            var cancel = ClinicalUIFactory.CreateButton("CancelPhobia", card.transform, "Back", false);
            ClinicalUIFactory.Anchor(cancel.GetComponent<RectTransform>(), new Vector2(0.32f, 0.10f), new Vector2(0.68f, 0.10f), new Vector2(0, 46));
            cancel.onClick.AddListener(() =>
            {
                if (detailRoot != null)
                    Destroy(detailRoot);
                detailRoot = null;
                SetBrowseVisible(true);
            });
        }

        private void CacheBrowseRoot()
        {
            if (browseRoot != null)
                return;
            if (heightPhobiaButton != null)
                browseRoot = heightPhobiaButton.transform.parent != null
                    ? heightPhobiaButton.transform.parent.gameObject
                    : heightPhobiaButton.gameObject;
        }

        private void SetBrowseVisible(bool visible)
        {
            CacheBrowseRoot();
            if (browseRoot != null && browseRoot.GetComponent<Canvas>() == null)
                browseRoot.SetActive(visible);

            if (heightPhobiaButton != null)
                heightPhobiaButton.gameObject.SetActive(visible);
            if (darknessPhobiaButton != null)
                darknessPhobiaButton.gameObject.SetActive(visible);
            if (crowdPhobiaButton != null)
                crowdPhobiaButton.gameObject.SetActive(visible);
            if (instructionText != null)
                instructionText.gameObject.SetActive(visible);
            if (selectedPhobiaText != null)
                selectedPhobiaText.gameObject.SetActive(visible);

            var back = AutoBindField<Button>("BackDashButton");
            if (back != null)
                back.gameObject.SetActive(visible);
        }

        private void ConfirmSelection(string difficulty)
        {
            UserData.SelectedPhobia = PhobiaCatalog.Normalize(pendingPhobia);
            UserData.SelectedDifficulty = UserData.AllowManualStageSelection ? difficulty : "High";
            UserData.CurrentStage = UserData.AllowManualStageSelection
                ? PhobiaCatalog.StageFromDifficulty(difficulty)
                : 1;
            UserData.ResetSessionState();
            UserData.AiStageRecommendation = "";
            SetBrowseVisible(false);
            if (detailRoot != null)
            {
                Destroy(detailRoot);
                detailRoot = null;
            }
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
