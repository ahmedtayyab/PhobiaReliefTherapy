using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Text.RegularExpressions;
using PhobiaReliefTherapy.Data;
using PhobiaReliefTherapy.Managers;
using System.Collections;
using UnityEngine.SceneManagement;

namespace PhobiaReliefTherapy.Managers
{
    /// <summary>
    /// Handles User Authentication (Login and Registration).
    /// Now uses Supabase for cloud-based auth and data.
    /// </summary>
    public class AuthManager : MonoBehaviour
    {
        [Header("Login UI")]
        public TMP_InputField loginEmailInput;
        public TMP_InputField loginPasswordInput;
        public Button loginButton;
        public TextMeshProUGUI loginErrorText;

        [Header("Register UI")]
        public TMP_InputField registerNameInput;
        public TMP_InputField registerEmailInput;
        public TMP_InputField registerPasswordInput;
        public Button registerButton;
        public TextMeshProUGUI registerErrorText;

        [Header("Navigation")]
        public Button goToRegisterButton;
        public Button goToLoginButton;

        private void Start()
        {
            // Setup listeners
            if (loginButton != null)
                loginButton.onClick.AddListener(() => OnLoginClicked());
            
            if (registerButton != null)
                registerButton.onClick.AddListener(() => OnRegisterClicked());

            if (goToRegisterButton != null)
                goToRegisterButton.onClick.AddListener(() => NavigateToScene("RegisterScene"));

            if (goToLoginButton != null)
                goToLoginButton.onClick.AddListener(() => NavigateToScene("LoginScene"));

            // Clear errors
            if (loginErrorText != null) loginErrorText.text = "";
            if (registerErrorText != null) registerErrorText.text = "";
        }

        private bool IsValidEmail(string email)
        {
            if (string.IsNullOrEmpty(email))
                return false;

            return Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$");
        }

        private bool IsValidPassword(string password)
        {
            if (string.IsNullOrEmpty(password) || password.Length < 8)
                return false;

            bool hasDigit = false;
            bool hasLetter = false;

            foreach (char c in password)
            {
                if (char.IsDigit(c)) hasDigit = true;
                if (char.IsLetter(c)) hasLetter = true;
            }

            return hasDigit && hasLetter;
        }

        private bool IsValidUsername(string username)
        {
            return !string.IsNullOrEmpty(username) && username.Length >= 3 && username.Length <= 30;
        }

        public void OnLoginClicked()
        {
            if (loginEmailInput == null || loginPasswordInput == null || loginErrorText == null || loginButton == null)
            {
                Debug.LogError("AuthManager login fields are not assigned in the inspector.");
                return;
            }

            string email = loginEmailInput.text.Trim();
            string pass = loginPasswordInput.text.Trim();

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(pass))
            {
                loginErrorText.text = "Email and password cannot be empty!";
                return;
            }

            if (!IsValidEmail(email))
            {
                loginErrorText.text = "Enter a valid email address.";
                return;
            }

            loginButton.interactable = false;
            loginErrorText.text = "Loading...";
            StartCoroutine(LoginCoroutine(email, pass));
        }

        public void OnRegisterClicked()
        {
            if (registerNameInput == null || registerEmailInput == null || registerPasswordInput == null || registerErrorText == null || registerButton == null)
            {
                Debug.LogError("AuthManager register fields are not assigned in the inspector.");
                return;
            }

            string name = registerNameInput.text.Trim();
            string email = registerEmailInput.text.Trim();
            string pass = registerPasswordInput.text.Trim();

            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(email) || string.IsNullOrEmpty(pass))
            {
                registerErrorText.text = "All fields are required!";
                return;
            }

            if (!IsValidUsername(name))
            {
                registerErrorText.text = "Name must be 3-30 characters.";
                return;
            }

            if (!IsValidEmail(email))
            {
                registerErrorText.text = "Enter a valid email address.";
                return;
            }

            if (!IsValidPassword(pass))
            {
                registerErrorText.text = "Password must be at least 8 characters and include both letters and numbers.";
                return;
            }

            registerButton.interactable = false;
            registerErrorText.text = "Loading, confirm email";
            StartCoroutine(RegisterCoroutine(name, email, pass));
        }

        private IEnumerator LoginCoroutine(string email, string password)
        {
            bool loginComplete = false;
            User loggedInUser = null;
            string loginError = "Invalid email or password!";

            yield return DatabaseManager.Instance.LoginUser(email, password, (user, error) =>
            {
                loggedInUser = user;
                if (!string.IsNullOrEmpty(error))
                    loginError = error;
                loginComplete = true;
            });

            // Wait for callback
            while (!loginComplete)
            {
                yield return null;
            }

            if (loggedInUser != null)
            {
                Data.UserData.UserId = loggedInUser.Id;
                Data.UserData.Username = loggedInUser.Username;
                Debug.Log($"User logged in: {loggedInUser.Username}");
                loginErrorText.text = "Login successful!";
                NavigateToScene("DashboardScene");
            }
            else
            {
                loginErrorText.text = loginError;
            }

            if (loginButton != null)
                loginButton.interactable = true;
        }

        private IEnumerator RegisterCoroutine(string name, string email, string password)
        {
            bool registerComplete = false;
            bool registerSuccess = false;
            string registerError = "Registration failed! Email may already be in use.";

            yield return DatabaseManager.Instance.RegisterUser(name, password, email, (success, error) =>
            {
                registerSuccess = success;
                if (!success && !string.IsNullOrEmpty(error))
                    registerError = error;
                registerComplete = true;
            });

            // Wait for callback
            while (!registerComplete)
            {
                yield return null;
            }

            if (registerSuccess)
            {
                registerErrorText.text = "Registered successfully";
                // Do not auto-login until the user has confirmed their email.
            }
            else
            {
                registerErrorText.text = registerError;
            }

            if (registerButton != null)
                registerButton.interactable = true;
        }

        private void NavigateToScene(string sceneName)
        {
            if (SceneLoader.Instance != null)
                SceneLoader.Instance.LoadScene(sceneName);
            else
                SceneManager.LoadScene(sceneName);
        }
    }
}