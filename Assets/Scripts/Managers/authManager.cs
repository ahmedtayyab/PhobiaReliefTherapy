using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace PhobiaReliefTherapy.Managers
{
    /// <summary>
    /// Handles User Authentication (Login and Registration).
    /// Currently uses simple local simulation instead of a real database.
    /// </summary>
    public class AuthManager : MonoBehaviour
    {
        [Header("Login UI")]
        public TMP_InputField loginUsernameInput;
        public TMP_InputField loginPasswordInput;
        public Button loginButton;
        public TextMeshProUGUI loginErrorText;

        [Header("Register UI")]
        public TMP_InputField registerNameInput;
        public TMP_InputField registerEmailInput;
        public TMP_InputField registerPasswordInput;
        public Button registerButton;
        public TextMeshProUGUI registerErrorText;

        private void Start()
        {
            // Setup listeners
            if (loginButton != null)
                loginButton.onClick.AddListener(OnLoginClicked);
            
            if (registerButton != null)
                registerButton.onClick.AddListener(OnRegisterClicked);

            // Clear errors
            if (loginErrorText != null) loginErrorText.text = "";
            if (registerErrorText != null) registerErrorText.text = "";
        }

        public void OnLoginClicked()
        {
            string user = loginUsernameInput.text;
            string pass = loginPasswordInput.text;

            if (string.IsNullOrEmpty(user) || string.IsNullOrEmpty(pass))
            {
                loginErrorText.text = "Username and password cannot be empty!";
                return;
            }

            // Simulated Authentication check
            Debug.Log($"User logged in: {user}");
            
            // Store simple user data
            Data.UserData.Username = user;

            // Load Dashboard
            SceneLoader.Instance.LoadScene("DashboardScene");
        }

        public void OnRegisterClicked()
        {
            string name = registerNameInput.text;
            string email = registerEmailInput.text;
            string pass = registerPasswordInput.text;

            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(email) || string.IsNullOrEmpty(pass))
            {
                registerErrorText.text = "All fields are required!";
                return;
            }

            // Simulated Registration
            Debug.Log($"User registered: {name} ({email})");
            
            // Store data
            Data.UserData.Username = name;

            // Go to dashboard
            SceneLoader.Instance.LoadScene("DashboardScene");
        }
    }
}