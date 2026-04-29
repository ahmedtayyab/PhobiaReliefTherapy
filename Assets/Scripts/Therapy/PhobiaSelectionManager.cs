using UnityEngine;
using UnityEngine.UI;
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

        private void Start()
        {
            if (heightPhobiaButton != null)
                heightPhobiaButton.onClick.AddListener(() => SelectPhobia("Height"));
                
            if (darknessPhobiaButton != null)
                darknessPhobiaButton.onClick.AddListener(() => SelectPhobia("Darkness"));
                
            if (crowdPhobiaButton != null)
                crowdPhobiaButton.onClick.AddListener(() => SelectPhobia("Crowd"));
        }

        private void SelectPhobia(string phobiaType)
        {
            Debug.Log($"Phobia Selected: {phobiaType}");
            UserData.SelectedPhobia = phobiaType;
            
            // After selecting, move to Baseline measurement
            SceneLoader.Instance.LoadScene("BaselineScene");
        }
    }
}
