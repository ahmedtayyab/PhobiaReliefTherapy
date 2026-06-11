using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using PhobiaReliefTherapy.Managers;
using PhobiaReliefTherapy.UI;

public static class AuthSceneBuilder
{
    [MenuItem("Tools/Build Auth Scenes/Build Welcome Scene")]
    private static void BuildWelcomeScene()
    {
        BuildWelcomeSceneAsset("Assets/Scenes/WelcomeScene.unity");
    }

    [MenuItem("Tools/Build Auth Scenes/Build Login Scene")]
    private static void BuildLoginScene()
    {
        BuildLoginSceneAsset("Assets/Scenes/LoginScene.unity");
    }

    [MenuItem("Tools/Build Auth Scenes/Build Register Scene")]
    private static void BuildRegisterScene()
    {
        BuildRegisterSceneAsset("Assets/Scenes/RegisterScene.unity");
    }

    [MenuItem("Tools/Build Auth Scenes/Build All Auth Scenes")]
    private static void BuildAllAuthScenes()
    {
        BuildWelcomeSceneAsset("Assets/Scenes/WelcomeScene.unity");
        BuildLoginSceneAsset("Assets/Scenes/LoginScene.unity");
        BuildRegisterSceneAsset("Assets/Scenes/RegisterScene.unity");
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("Auth Scenes Created", "Welcome, Login and Register scenes have been rebuilt in Assets/Scenes.", "OK");
    }



    private static void BuildWelcomeSceneAsset(string scenePath)
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        CreateMainCamera();
        GameObject canvasGO = CreateCanvas();

        CreateBackground(canvasGO.transform);
        GameObject card = CreateCard(canvasGO.transform, new Vector2(1100, 640));

        var cardFade = card.AddComponent<UIFadeIn>();
        cardFade.delay = 0f;
        cardFade.duration = 0.5f;
        cardFade.scaleUp = true;
        cardFade.initialScale = 0.97f;

        GameObject title = CreateText("AppTitle", card.transform, "Phobia Relief Therapy", 56, TextAlignmentOptions.Center, Color.white);
        var titleFade = title.AddComponent<UIFadeIn>();
        titleFade.delay = 0.15f;
        titleFade.duration = 0.5f;

        GameObject subtitle = CreateText("AppSubtitle", card.transform, "A calm, modern VR therapy platform for phobia recovery.", 26, TextAlignmentOptions.Center, Color.white);
        var subtitleFade = subtitle.AddComponent<UIFadeIn>();
        subtitleFade.delay = 0.4f;
        subtitleFade.duration = 0.5f;

        GameObject footer = CreateText("FooterText", card.transform, "Professional, consistent UI across every scene.", 18, TextAlignmentOptions.Center, Color.white);
        var footerFade = footer.AddComponent<UIFadeIn>();
        footerFade.delay = 0.9f;
        footerFade.duration = 0.5f;

        GameObject getStarted = CreateButton("GetStartedButton", card.transform, "Get Started");
        var btnFade = getStarted.AddComponent<UIFadeIn>();
        btnFade.delay = 0.65f;
        btnFade.duration = 0.5f;
        btnFade.scaleUp = true;
        btnFade.initialScale = 0.9f;

        ArrangeWelcomeLayout(title.GetComponent<RectTransform>(), subtitle.GetComponent<RectTransform>(), getStarted.GetComponent<RectTransform>(), footer.GetComponent<RectTransform>());

        var manager = card.AddComponent<WelcomeScreenManager>();
        manager.appTitle = title.GetComponent<TextMeshProUGUI>();
        manager.appSubtitle = subtitle.GetComponent<TextMeshProUGUI>();
        manager.footerText = footer.GetComponent<TextMeshProUGUI>();
        manager.getStartedButton = getStarted.GetComponent<Button>();

        SaveScene(scene, scenePath);
    }

    private static void BuildLoginSceneAsset(string scenePath)
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        CreateMainCamera();
        GameObject canvasGO = CreateCanvas();

        CreateBackground(canvasGO.transform);
        GameObject card = CreateCard(canvasGO.transform, new Vector2(700, 540));
        
        var cardFade = card.AddComponent<UIFadeIn>();
        cardFade.delay = 0f;
        cardFade.duration = 0.5f;
        cardFade.scaleUp = true;
        cardFade.initialScale = 0.97f;
        
        GameObject title = CreateText("LoginTitle", card.transform, "Login", 42, TextAlignmentOptions.Center, Color.white);
        GameObject emailField = CreateInputField("EmailInput", card.transform, "Enter Email");
        GameObject passwordField = CreateInputField("PasswordInput", card.transform, "Enter Password", true);
        GameObject forgotPasswordButton = CreateTextButton("ForgotPasswordButton", card.transform, "Forgot password?");
        GameObject loginButton = CreateButton("LoginButton", card.transform, "Login");
        GameObject createAccountButton = CreateTextButton("CreateAccountButton", card.transform, "Create new account");
        GameObject errorText = CreateText("LoginErrorText", card.transform, "", 18, TextAlignmentOptions.Center, new Color32(229, 62, 62, 255));

        ArrangeLoginLayout(
            title.GetComponent<RectTransform>(),
            emailField.GetComponent<RectTransform>(),
            passwordField.GetComponent<RectTransform>(),
            forgotPasswordButton.GetComponent<RectTransform>(),
            loginButton.GetComponent<RectTransform>(),
            createAccountButton.GetComponent<RectTransform>(),
            errorText.GetComponent<RectTransform>());

        var managerGO = new GameObject("AuthManagerObject", typeof(AuthManager));
        managerGO.transform.SetParent(card.transform, false);
        var auth = managerGO.GetComponent<AuthManager>();
        auth.loginEmailInput = emailField.GetComponent<TMP_InputField>();
        auth.loginPasswordInput = passwordField.GetComponent<TMP_InputField>();
        auth.loginButton = loginButton.GetComponent<Button>();
        auth.loginErrorText = errorText.GetComponent<TextMeshProUGUI>();
        auth.goToRegisterButton = createAccountButton.GetComponent<Button>();
        auth.forgotPasswordButton = forgotPasswordButton.GetComponent<Button>();

        SaveScene(scene, scenePath);
    }

    private static void BuildRegisterSceneAsset(string scenePath)
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        CreateMainCamera();
        GameObject canvasGO = CreateCanvas();

        CreateBackground(canvasGO.transform);
        GameObject card = CreateCard(canvasGO.transform, new Vector2(700, 640));

        var cardFade = card.AddComponent<UIFadeIn>();
        cardFade.delay = 0f;
        cardFade.duration = 0.5f;
        cardFade.scaleUp = true;
        cardFade.initialScale = 0.97f;

        GameObject title = CreateText("RegisterTitle", card.transform, "Create Account", 42, TextAlignmentOptions.Center, Color.white);
        GameObject nameField = CreateInputField("NameInput", card.transform, "Enter Full Name");
        GameObject emailField = CreateInputField("EmailInput", card.transform, "Enter Email");
        GameObject passwordField = CreateInputField("PasswordInput", card.transform, "Create Password", true);
        GameObject registerButton = CreateButton("RegisterButton", card.transform, "Register");
        GameObject loginButton = CreateTextButton("BackToLoginButton", card.transform, "Already have an account? Login");
        GameObject errorText = CreateText("RegisterErrorText", card.transform, "", 18, TextAlignmentOptions.Center, new Color32(229, 62, 62, 255));

        ArrangeRegisterLayout(title.GetComponent<RectTransform>(), nameField.GetComponent<RectTransform>(), emailField.GetComponent<RectTransform>(), passwordField.GetComponent<RectTransform>(), registerButton.GetComponent<RectTransform>(), errorText.GetComponent<RectTransform>(), loginButton.GetComponent<RectTransform>());

        var managerGO = new GameObject("AuthManagerObject", typeof(AuthManager));
        managerGO.transform.SetParent(card.transform, false);
        var auth = managerGO.GetComponent<AuthManager>();
        auth.registerNameInput = nameField.GetComponent<TMP_InputField>();
        auth.registerEmailInput = emailField.GetComponent<TMP_InputField>();
        auth.registerPasswordInput = passwordField.GetComponent<TMP_InputField>();
        auth.registerButton = registerButton.GetComponent<Button>();
        auth.registerErrorText = errorText.GetComponent<TextMeshProUGUI>();
        auth.goToLoginButton = loginButton.GetComponent<Button>();

        SaveScene(scene, scenePath);
    }

    private static GameObject CreateCanvas()
    {
        var canvasGO = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        CreateEventSystem();
        return canvasGO;
    }

    private static void CreateEventSystem()
    {
        if (Object.FindObjectOfType<EventSystem>() != null)
            return;

        var eventSystemGO = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        eventSystemGO.transform.position = Vector3.zero;
    }

    private static void CreateBackground(Transform parent)
    {
        var background = new GameObject("Background", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(UIGradient));
        background.transform.SetParent(parent, false);
        var bgRect = background.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;
        var bgImage = background.GetComponent<Image>();
        bgImage.color = new Color32(16, 46, 84, 255);
        var gradient = background.GetComponent<UIGradient>();
        gradient.topColor = new Color32(42, 111, 172, 255);
        gradient.bottomColor = new Color32(12, 21, 45, 255);
        gradient.vertical = true;
    }

    private static GameObject CreateCard(Transform parent, Vector2 size)
    {
        var card = new GameObject("Card", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        card.transform.SetParent(parent, false);
        var cardRect = card.GetComponent<RectTransform>();
        cardRect.sizeDelta = size;
        cardRect.anchorMin = new Vector2(0.5f, 0.5f);
        cardRect.anchorMax = new Vector2(0.5f, 0.5f);
        cardRect.anchoredPosition = Vector2.zero;
        cardRect.pivot = new Vector2(0.5f, 0.5f);
        var cardImage = card.GetComponent<Image>();
        cardImage.color = new Color32(255, 255, 255, 245);
        return card;
    }

    private static GameObject CreateText(string name, Transform parent, string text, int fontSize, TextAlignmentOptions alignment, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.alignment = alignment;
        tmp.enableWordWrapping = true;
        tmp.color = color;
        return go;
    }

    private static GameObject CreateButton(string name, Transform parent, string label)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        var image = go.GetComponent<Image>();
        image.color = new Color32(28, 83, 146, 255);
        var text = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        text.transform.SetParent(go.transform, false);
        var tmp = text.GetComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 24;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.enableWordWrapping = false;
        var rect = go.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(240, 54);
        var textRect = text.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        return go;
    }

    private static GameObject CreateTextButton(string name, Transform parent, string label)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Button));
        go.transform.SetParent(parent, false);
        var text = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        text.transform.SetParent(go.transform, false);
        var tmp = text.GetComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 18;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.enableWordWrapping = true;
        var rect = go.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(360, 40);
        var textRect = text.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        return go;
    }

    private static GameObject CreateInputField(string name, Transform parent, string placeholderText, bool isPassword = false)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(TMP_InputField));
        go.transform.SetParent(parent, false);
        var background = go.GetComponent<Image>();
        background.color = new Color32(255, 255, 255, 230);
        var input = go.GetComponent<TMP_InputField>();
        var textGO = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textGO.transform.SetParent(go.transform, false);
        var textComp = textGO.GetComponent<TextMeshProUGUI>();
        textComp.fontSize = 16;
        textComp.color = Color.white;
        textComp.alignment = TextAlignmentOptions.Left;
        input.textViewport = go.GetComponent<RectTransform>();
        input.textComponent = textComp;
        input.contentType = isPassword ? TMP_InputField.ContentType.Password : TMP_InputField.ContentType.Standard;
        var placeholderGO = new GameObject("Placeholder", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        placeholderGO.transform.SetParent(go.transform, false);
        var placeholder = placeholderGO.GetComponent<TextMeshProUGUI>();
        placeholder.text = placeholderText;
        placeholder.fontSize = 16;
        placeholder.color = new Color32(200, 200, 210, 255);
        placeholder.alignment = TextAlignmentOptions.Left;
        input.placeholder = placeholder;
        var rect = go.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(600, 40);
        var textRect = textGO.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(16, 8);
        textRect.offsetMax = new Vector2(-16, -8);
        var placeholderRect = placeholderGO.GetComponent<RectTransform>();
        placeholderRect.anchorMin = Vector2.zero;
        placeholderRect.anchorMax = Vector2.one;
        placeholderRect.offsetMin = new Vector2(16, 8);
        placeholderRect.offsetMax = new Vector2(-16, -8);
        return go;
    }



    private static void ArrangeLoginLayout(RectTransform title, RectTransform email, RectTransform password, RectTransform forgotPasswordButton, RectTransform loginButton, RectTransform createAccountButton, RectTransform errorText)
    {
        title.anchorMin = new Vector2(0.08f, 0.80f);
        title.anchorMax = new Vector2(0.92f, 0.90f);
        title.offsetMin = Vector2.zero;
        title.offsetMax = Vector2.zero;

        email.anchorMin = new Vector2(0.20f, 0.63f);
        email.anchorMax = new Vector2(0.80f, 0.63f);
        email.sizeDelta = new Vector2(0, 48);
        email.anchoredPosition = Vector2.zero;

        password.anchorMin = new Vector2(0.20f, 0.47f);
        password.anchorMax = new Vector2(0.80f, 0.47f);
        password.sizeDelta = new Vector2(0, 48);
        password.anchoredPosition = Vector2.zero;

        forgotPasswordButton.anchorMin = new Vector2(0.20f, 0.37f);
        forgotPasswordButton.anchorMax = new Vector2(0.80f, 0.37f);
        forgotPasswordButton.sizeDelta = new Vector2(0, 28);
        forgotPasswordButton.anchoredPosition = Vector2.zero;

        loginButton.anchorMin = new Vector2(0.5f, 0.28f);
        loginButton.anchorMax = new Vector2(0.5f, 0.28f);
        loginButton.sizeDelta = new Vector2(240, 54);
        loginButton.anchoredPosition = Vector2.zero;

        createAccountButton.anchorMin = new Vector2(0.10f, 0.16f);
        createAccountButton.anchorMax = new Vector2(0.90f, 0.16f);
        createAccountButton.sizeDelta = new Vector2(0, 30);
        createAccountButton.anchoredPosition = Vector2.zero;

        errorText.anchorMin = new Vector2(0.10f, 0.06f);
        errorText.anchorMax = new Vector2(0.90f, 0.06f);
        errorText.sizeDelta = new Vector2(0, 30);
        errorText.anchoredPosition = Vector2.zero;
    }

    private static void ArrangeRegisterLayout(RectTransform title, RectTransform name, RectTransform email, RectTransform password, RectTransform registerButton, RectTransform errorText, RectTransform loginButton)
    {
        title.anchorMin = new Vector2(0.08f, 0.84f);
        title.anchorMax = new Vector2(0.92f, 0.94f);
        title.offsetMin = Vector2.zero;
        title.offsetMax = Vector2.zero;

        name.anchorMin = new Vector2(0.20f, 0.70f);
        name.anchorMax = new Vector2(0.80f, 0.70f);
        name.sizeDelta = new Vector2(0, 48);
        name.anchoredPosition = Vector2.zero;

        email.anchorMin = new Vector2(0.20f, 0.55f);
        email.anchorMax = new Vector2(0.80f, 0.55f);
        email.sizeDelta = new Vector2(0, 48);
        email.anchoredPosition = Vector2.zero;

        password.anchorMin = new Vector2(0.20f, 0.40f);
        password.anchorMax = new Vector2(0.80f, 0.40f);
        password.sizeDelta = new Vector2(0, 48);
        password.anchoredPosition = Vector2.zero;

        registerButton.anchorMin = new Vector2(0.5f, 0.25f);
        registerButton.anchorMax = new Vector2(0.5f, 0.25f);
        registerButton.sizeDelta = new Vector2(240, 54);
        registerButton.anchoredPosition = Vector2.zero;

        loginButton.anchorMin = new Vector2(0.10f, 0.14f);
        loginButton.anchorMax = new Vector2(0.90f, 0.14f);
        loginButton.sizeDelta = new Vector2(0, 30);
        loginButton.anchoredPosition = Vector2.zero;

        errorText.anchorMin = new Vector2(0.10f, 0.06f);
        errorText.anchorMax = new Vector2(0.90f, 0.06f);
        errorText.sizeDelta = new Vector2(0, 30);
        errorText.anchoredPosition = Vector2.zero;
    }

    private static void ArrangeWelcomeLayout(RectTransform title, RectTransform subtitle, RectTransform primary, RectTransform footer)
    {
        title.anchorMin = new Vector2(0.08f, 0.70f);
        title.anchorMax = new Vector2(0.92f, 0.92f);
        title.offsetMin = Vector2.zero;
        title.offsetMax = Vector2.zero;
        subtitle.anchorMin = new Vector2(0.12f, 0.58f);
        subtitle.anchorMax = new Vector2(0.88f, 0.70f);
        subtitle.offsetMin = Vector2.zero;
        subtitle.offsetMax = Vector2.zero;
        primary.anchorMin = new Vector2(0.5f, 0.43f);
        primary.anchorMax = new Vector2(0.5f, 0.43f);
        primary.sizeDelta = new Vector2(240, 54);
        primary.anchoredPosition = Vector2.zero;
        footer.anchorMin = new Vector2(0.12f, 0.08f);
        footer.anchorMax = new Vector2(0.88f, 0.18f);
        footer.offsetMin = Vector2.zero;
        footer.offsetMax = Vector2.zero;
    }

    private static void SaveScene(Scene scene, string scenePath)
    {
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, scenePath);
    }

    private static void CreateMainCamera()
    {
        var cameraGO = new GameObject("Main Camera", typeof(Camera));
        cameraGO.tag = "MainCamera";
        cameraGO.transform.position = new Vector3(0, 1, -10);
        var camera = cameraGO.GetComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color32(13, 14, 18, 255); // #0D0E12 charcoal
    }
}
