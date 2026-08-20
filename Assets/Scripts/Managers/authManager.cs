using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Text.RegularExpressions;
using PhobiaReliefTherapy.Data;
using PhobiaReliefTherapy.Managers;
using System.Collections;
using UnityEngine.SceneManagement;
using PhobiaReliefTherapy;
using UnityEngine.XR;

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

        [Header("Login Recovery")]
        public Button forgotPasswordButton;
        public Button forgotUsernameButton;

        private void Start()
        {
            VRUIInputBridge.EnsureInstanceExists();
            AutoBindMissingFields();
            EnsureLoginRecoveryButtons();

            // Setup listeners
            if (loginButton != null)
                loginButton.onClick.AddListener(() => OnLoginClicked());
            
            if (registerButton != null)
                registerButton.onClick.AddListener(() => OnRegisterClicked());

            if (goToRegisterButton != null)
                goToRegisterButton.onClick.AddListener(() => NavigateToScene("RegisterScene"));

            if (goToLoginButton != null)
                goToLoginButton.onClick.AddListener(() => NavigateToScene("LoginScene"));

            if (forgotPasswordButton != null)
                forgotPasswordButton.onClick.AddListener(() => OnForgotPasswordClicked());

            if (forgotUsernameButton != null)
                forgotUsernameButton.onClick.AddListener(() => OnForgotUsernameClicked());

            // Clear errors
            if (loginErrorText != null) loginErrorText.text = "";
            if (registerErrorText != null) registerErrorText.text = "";
        }

        private void AutoBindMissingFields()
        {
            bool isRegisterPage = (registerButton != null) || 
                                  (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "RegisterScene");

            if (isRegisterPage)
            {
                if (registerNameInput == null)
                    registerNameInput = AutoBindHelper.FindComponentByName<TMP_InputField>("NameInput");
                if (registerEmailInput == null)
                    registerEmailInput = AutoBindHelper.FindComponentByName<TMP_InputField>("EmailInput");
                if (registerPasswordInput == null)
                    registerPasswordInput = AutoBindHelper.FindComponentByName<TMP_InputField>("PasswordInput");
                if (registerButton == null)
                    registerButton = AutoBindHelper.FindComponentByName<Button>("RegisterButton");
                if (registerErrorText == null)
                    registerErrorText = AutoBindHelper.FindComponentByName<TextMeshProUGUI>("RegisterErrorText");
                if (goToLoginButton == null)
                    goToLoginButton = AutoBindHelper.FindComponentByName<Button>("BackToLoginButton");
                return;
            }

            if (loginEmailInput == null)
                loginEmailInput = AutoBindHelper.FindComponentByName<TMP_InputField>("EmailInput");
            if (loginPasswordInput == null)
                loginPasswordInput = AutoBindHelper.FindComponentByName<TMP_InputField>("PasswordInput");
            if (loginButton == null)
                loginButton = AutoBindHelper.FindComponentByName<Button>("LoginButton");
            if (loginErrorText == null)
                loginErrorText = AutoBindHelper.FindComponentByName<TextMeshProUGUI>("LoginErrorText");
            if (goToRegisterButton == null)
                goToRegisterButton = AutoBindHelper.FindComponentByName<Button>("CreateAccountButton");
            if (forgotPasswordButton == null)
                forgotPasswordButton = AutoBindHelper.FindComponentByName<Button>("ForgotPasswordButton");
        }

        private void EnsureLoginRecoveryButtons()
        {
            if (loginEmailInput == null || forgotPasswordButton != null)
                return;

            Transform card = loginEmailInput.transform.parent;
            if (card == null)
                return;

            if (forgotPasswordButton == null)
            {
                GameObject go = CreateRecoveryTextButton("ForgotPasswordButton", card, "Forgot password?");
                forgotPasswordButton = go.GetComponent<Button>();
                PositionRecoveryButton(go.GetComponent<RectTransform>(), 0.20f, 0.80f, 0.37f);
            }
        }

        private static GameObject CreateRecoveryTextButton(string name, Transform parent, string label)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Button));
            go.transform.SetParent(parent, false);

            var textGO = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            textGO.transform.SetParent(go.transform, false);

            var tmp = textGO.GetComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 16;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color32(28, 83, 146, 255);
            tmp.enableWordWrapping = false;

            var rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(280, 28);

            var textRect = textGO.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            return go;
        }

        private static void PositionRecoveryButton(RectTransform rect, float anchorMinX, float anchorMaxX, float anchorY)
        {
            rect.anchorMin = new Vector2(anchorMinX, anchorY);
            rect.anchorMax = new Vector2(anchorMaxX, anchorY);
            rect.sizeDelta = new Vector2(0, 28);
            rect.anchoredPosition = Vector2.zero;
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

            loginErrorText.color = new Color32(229, 62, 62, 255);

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
                registerErrorText.color = new Color32(229, 62, 62, 255); // Red
                registerErrorText.text = "All fields are required!";
                return;
            }

            if (!IsValidUsername(name))
            {
                registerErrorText.color = new Color32(229, 62, 62, 255); // Red
                registerErrorText.text = "Name must be 3-30 characters.";
                return;
            }

            if (!IsValidEmail(email))
            {
                registerErrorText.color = new Color32(229, 62, 62, 255); // Red
                registerErrorText.text = "Enter a valid email address.";
                return;
            }

            if (!IsValidPassword(pass))
            {
                registerErrorText.color = new Color32(229, 62, 62, 255); // Red
                registerErrorText.text = "Password must be at least 8 characters and include both letters and numbers.";
                return;
            }

            registerButton.interactable = false;
            registerErrorText.color = new Color32(28, 83, 146, 255); // Neutral blue
            registerErrorText.text = "Loading, confirm email...";
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
                Data.UserData.IsAdmin = IsAdminAccount(email, loggedInUser.Username);
                Debug.Log($"User logged in: {loggedInUser.Username}");
                loginErrorText.text = "Login successful!";
                NavigateToScene(Data.UserData.IsAdmin ? "AdminScene" : "DashboardScene");
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
                registerErrorText.color = new Color32(72, 187, 120, 255); // Green for success
                registerErrorText.text = "Registered successfully! Check your email to confirm your account.";
                // Do not auto-login until the user has confirmed their email.
            }
            else
            {
                registerErrorText.color = new Color32(229, 62, 62, 255); // Red for error
                registerErrorText.text = registerError;
            }

            if (registerButton != null)
                registerButton.interactable = true;
        }

        public void OnForgotPasswordClicked()
        {
            if (loginEmailInput == null || loginErrorText == null)
                return;

            string email = loginEmailInput.text.Trim();

            if (string.IsNullOrEmpty(email))
            {
                loginErrorText.text = "Enter your email above, then tap Forgot password.";
                return;
            }

            if (!IsValidEmail(email))
            {
                loginErrorText.text = "Enter a valid email address.";
                return;
            }

            if (forgotPasswordButton != null)
                forgotPasswordButton.interactable = false;
            if (forgotUsernameButton != null)
                forgotUsernameButton.interactable = false;

            loginErrorText.text = "Sending reset link...";
            StartCoroutine(ForgotPasswordCoroutine(email));
        }

        public void OnForgotUsernameClicked()
        {
            if (loginEmailInput == null || loginErrorText == null)
                return;

            string email = loginEmailInput.text.Trim();

            if (string.IsNullOrEmpty(email))
            {
                loginErrorText.text = "Enter your email above, then tap Forgot username.";
                return;
            }

            if (!IsValidEmail(email))
            {
                loginErrorText.text = "Enter a valid email address.";
                return;
            }

            if (forgotPasswordButton != null)
                forgotPasswordButton.interactable = false;
            if (forgotUsernameButton != null)
                forgotUsernameButton.interactable = false;

            loginErrorText.text = "Looking up account...";
            StartCoroutine(ForgotUsernameCoroutine(email));
        }

        private IEnumerator ForgotPasswordCoroutine(string email)
        {
            bool complete = false;
            bool success = false;
            string error = "Could not send password reset email. Try again later.";

            yield return DatabaseManager.Instance.RecoverPassword(email, (ok, err) =>
            {
                success = ok;
                if (!ok && !string.IsNullOrEmpty(err))
                    error = err;
                complete = true;
            });

            while (!complete)
                yield return null;

            if (success)
            {
                loginErrorText.text = "If an account exists for that email, a password reset link has been sent.";
                loginErrorText.color = new Color32(28, 83, 146, 255);
            }
            else
            {
                loginErrorText.text = error;
                loginErrorText.color = new Color32(229, 62, 62, 255);
            }

            if (forgotPasswordButton != null)
                forgotPasswordButton.interactable = true;
            if (forgotUsernameButton != null)
                forgotUsernameButton.interactable = true;
        }

        private IEnumerator ForgotUsernameCoroutine(string email)
        {
            bool complete = false;
            string username = null;
            string error = "Could not look up account. Try again later.";

            yield return DatabaseManager.Instance.LookupUsernameByEmail(email, (foundUsername, err) =>
            {
                username = foundUsername;
                if (!string.IsNullOrEmpty(err))
                    error = err;
                complete = true;
            });

            while (!complete)
                yield return null;

            if (!string.IsNullOrEmpty(username))
            {
                loginErrorText.text = $"Your account name is: {username}";
                loginErrorText.color = new Color32(28, 83, 146, 255);
            }
            else
            {
                loginErrorText.text = error;
                loginErrorText.color = new Color32(229, 62, 62, 255);
            }

            if (forgotPasswordButton != null)
                forgotPasswordButton.interactable = true;
            if (forgotUsernameButton != null)
                forgotUsernameButton.interactable = true;
        }

        private bool IsAdminAccount(string email, string username)
        {
            if (string.IsNullOrEmpty(email) && string.IsNullOrEmpty(username))
                return false;

            if (!string.IsNullOrEmpty(username) && username.Trim().Equals("admin", System.StringComparison.OrdinalIgnoreCase))
                return true;

            if (!string.IsNullOrEmpty(email) && email.Trim().Equals("admin@phobia.local", System.StringComparison.OrdinalIgnoreCase))
                return true;

            return false;
        }

        private void NavigateToScene(string sceneName)
        {
            if (SceneLoader.Instance != null)
                SceneLoader.Instance.LoadScene(sceneName);
            else
                SceneManager.LoadScene(sceneName);
        }
    }

    /// <summary>
    /// Lightweight VR-friendly UI bridge that lets headset gaze and controller trigger input click existing Unity UI.
    /// This avoids the need for a full XR Interaction Toolkit rig while still making login/register screens usable in VR.
    /// </summary>
    public class VRUIInputBridge : MonoBehaviour
    {
        private static VRUIInputBridge instance;

        private readonly System.Collections.Generic.List<RaycastResult> raycastResults = new System.Collections.Generic.List<RaycastResult>();
        private PointerEventData pointerEventData;
        private EventSystem eventSystem;
        private GameObject currentHoveredObject;
        private GameObject pressedObject;
        private bool wasPressedLastFrame;

        public static void EnsureInstanceExists()
        {
            if (instance != null)
                return;

            GameObject existing = Object.FindObjectOfType<VRUIInputBridge>()?.gameObject;
            if (existing != null)
            {
                instance = existing.GetComponent<VRUIInputBridge>();
                return;
            }

            GameObject go = new GameObject("VRUIInputBridge");
            instance = go.AddComponent<VRUIInputBridge>();
            Object.DontDestroyOnLoad(go);
        }

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                Object.DontDestroyOnLoad(gameObject);
            }
            else if (instance != this)
            {
                Destroy(gameObject);
                return;
            }

            SceneManager.sceneLoaded += OnSceneLoaded;
            EnsureEventSystemExists();
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                SceneManager.sceneLoaded -= OnSceneLoaded;
                instance = null;
            }
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode loadSceneMode)
        {
            EnsureEventSystemExists();
            currentHoveredObject = null;
            pressedObject = null;
            wasPressedLastFrame = false;
        }

        private void EnsureEventSystemExists()
        {
            if (UnityEngine.XR.XRSettings.isDeviceActive || 
                (UnityEngine.XR.Management.XRGeneralSettings.Instance != null && 
                 UnityEngine.XR.Management.XRGeneralSettings.Instance.Manager != null && 
                 UnityEngine.XR.Management.XRGeneralSettings.Instance.Manager.activeLoader != null))
            {
                return;
            }

            eventSystem = EventSystem.current != null ? EventSystem.current : Object.FindObjectOfType<EventSystem>();
            if (eventSystem == null)
            {
                GameObject eventSystemGO = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
                eventSystem = eventSystemGO.GetComponent<EventSystem>();
                Object.DontDestroyOnLoad(eventSystemGO);
            }
            else if (eventSystem.GetComponent<StandaloneInputModule>() == null)
            {
                eventSystem.gameObject.AddComponent<StandaloneInputModule>();
            }

            if (pointerEventData == null && eventSystem != null)
            {
                pointerEventData = new PointerEventData(eventSystem);
            }
        }

        private void Update()
        {
            // If native VR rig is active, disable this legacy gaze bridge to avoid input module conflicts and auto-deselection of fields
            if (UnityEngine.XR.XRSettings.isDeviceActive || 
                (UnityEngine.XR.Management.XRGeneralSettings.Instance != null && 
                 UnityEngine.XR.Management.XRGeneralSettings.Instance.Manager != null && 
                 UnityEngine.XR.Management.XRGeneralSettings.Instance.Manager.activeLoader != null))
            {
                if (eventSystem != null)
                {
                    var standalone = eventSystem.GetComponent<StandaloneInputModule>();
                    if (standalone != null)
                    {
                        Destroy(standalone);
                    }
                }
                gameObject.SetActive(false);
                return;
            }

            if (eventSystem == null)
            {
                EnsureEventSystemExists();
                if (eventSystem == null)
                    return;
            }

            if (pointerEventData == null)
            {
                pointerEventData = new PointerEventData(eventSystem);
            }

            pointerEventData.Reset();
            pointerEventData.position = GetPointerPosition();
            pointerEventData.button = PointerEventData.InputButton.Left;

            raycastResults.Clear();
            eventSystem.RaycastAll(pointerEventData, raycastResults);

            GameObject hoveredObject = raycastResults.Count > 0 ? raycastResults[0].gameObject : null;
            UpdateHoverState(hoveredObject);

            bool triggerPressed = IsTriggerPressed();
            if (triggerPressed && !wasPressedLastFrame)
            {
                HandlePointerDown(hoveredObject);
            }
            else if (!triggerPressed && wasPressedLastFrame)
            {
                HandlePointerUp(hoveredObject);
            }

            wasPressedLastFrame = triggerPressed;
        }

        private Vector2 GetPointerPosition()
        {
            if (Application.isEditor)
                return new Vector2(Input.mousePosition.x, Input.mousePosition.y);

            return new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        }

        private bool IsTriggerPressed()
        {
            if (Application.isEditor)
                return Input.GetMouseButton(0);

            return IsXRButtonPressed(XRNode.RightHand) || IsXRButtonPressed(XRNode.LeftHand);
        }

        private bool IsXRButtonPressed(XRNode node)
        {
            InputDevice device = InputDevices.GetDeviceAtXRNode(node);
            if (!device.isValid)
                return false;

            if (device.TryGetFeatureValue(CommonUsages.triggerButton, out bool triggerPressed) && triggerPressed)
                return true;

            if (device.TryGetFeatureValue(CommonUsages.primaryButton, out bool primaryPressed) && primaryPressed)
                return true;

            return false;
        }

        private void UpdateHoverState(GameObject hoveredObject)
        {
            if (hoveredObject == currentHoveredObject)
                return;

            if (currentHoveredObject != null)
            {
                ExecuteEvents.ExecuteHierarchy(currentHoveredObject, pointerEventData, ExecuteEvents.pointerExitHandler);
            }

            currentHoveredObject = hoveredObject;

            if (currentHoveredObject != null)
            {
                ExecuteEvents.ExecuteHierarchy(currentHoveredObject, pointerEventData, ExecuteEvents.pointerEnterHandler);
            }
        }

        private void HandlePointerDown(GameObject hoveredObject)
        {
            pressedObject = hoveredObject;
            if (pressedObject == null)
                return;

            ExecuteEvents.ExecuteHierarchy(pressedObject, pointerEventData, ExecuteEvents.pointerDownHandler);
            eventSystem.SetSelectedGameObject(pressedObject);
        }

        private void HandlePointerUp(GameObject hoveredObject)
        {
            if (pressedObject == null)
                return;

            ExecuteEvents.ExecuteHierarchy(pressedObject, pointerEventData, ExecuteEvents.pointerUpHandler);

            if (hoveredObject == pressedObject)
            {
                ExecuteEvents.ExecuteHierarchy(pressedObject, pointerEventData, ExecuteEvents.pointerClickHandler);
                ExecuteEvents.ExecuteHierarchy(pressedObject, pointerEventData, ExecuteEvents.submitHandler);
            }

            pressedObject = null;
        }
    }
}