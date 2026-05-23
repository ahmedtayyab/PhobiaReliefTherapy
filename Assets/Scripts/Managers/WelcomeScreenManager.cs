using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

namespace PhobiaReliefTherapy.Managers
{
    public class WelcomeScreenManager : MonoBehaviour
    {
        public TextMeshProUGUI appTitle;
        public TextMeshProUGUI appSubtitle;
        public TextMeshProUGUI footerText;
        public Button getStartedButton;

        private void Start()
        {
            if (getStartedButton != null)
            {
                getStartedButton.onClick.AddListener(() => NavigateToLogin());
            }
        }

        public void NavigateToLogin()
        {
            if (SceneLoader.Instance != null)
                SceneLoader.Instance.LoadScene("LoginScene");
            else
                SceneManager.LoadScene("LoginScene");
        }
    }
}
