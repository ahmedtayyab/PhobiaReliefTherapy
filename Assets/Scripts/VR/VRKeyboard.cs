using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit.UI;
using UnityEngine.SceneManagement;

namespace PhobiaReliefTherapy.VR
{
    /// <summary>
    /// A fully self-contained world-space on-screen keyboard for VR.
    /// Floats 0.6 m in front of the player, driven entirely by Unity UI buttons
    /// so the XR ray interactor can click each key without any OS-level keyboard API.
    /// </summary>
    public class VRKeyboard : MonoBehaviour
    {
        // ── Singleton ──────────────────────────────────────────────────────────
        public static VRKeyboard Instance { get; private set; }

        // Tag placed on the root GameObject so UIThemeAutoApply skips all children
        public const string KEYBOARD_ROOT_NAME = "VRKeyboard_Root";

        // ── Target field ───────────────────────────────────────────────────────
        private TMP_InputField  targetTMP;
        private InputField      targetLegacy;
        private bool            isPassword;

        // ── State ──────────────────────────────────────────────────────────────
        private string  buffer        = "";
        private bool    capsOn        = false;
        private GameObject keyboardRoot;
        private TextMeshProUGUI displayText;            // shows buffer in the "text bar"

        // ── Drag state ─────────────────────────────────────────────────────────
        private float  dragDistance   = 0.65f;   // metres from controller
        private XRNode draggingNode   = XRNode.RightHand;
        private readonly List<InputDevice> _deviceBuf = new List<InputDevice>();

        // ── Key layout ─────────────────────────────────────────────────────────
        private static readonly string[][] RowsNormal = new[]
        {
            new[] { "1","2","3","4","5","6","7","8","9","0","-" },
            new[] { "q","w","e","r","t","y","u","i","o","p" },
            new[] { "a","s","d","f","g","h","j","k","l",";" },
            new[] { "z","x","c","v","b","n","m",",","." }
        };

        private static readonly string[][] RowsShifted = new[]
        {
            new[] { "!","@","#","$","%","^","&","*","(",")","-" },
            new[] { "Q","W","E","R","T","Y","U","I","O","P" },
            new[] { "A","S","D","F","G","H","J","K","L",";" },
            new[] { "Z","X","C","V","B","N","M","<",">" }
        };

        // ── Initialise ─────────────────────────────────────────────────────────
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            // Auto-hide whenever the user navigates to any scene
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // Always close & reset when changing scenes so the keyboard
            // never bleeds from login/register into therapy scenes.
            targetTMP    = null;
            targetLegacy = null;
            buffer       = "";
            SetVisible(false);
        }

        private void Start()
        {
            BuildKeyboard();
            SetVisible(false);
        }

        private void Update()
        {
            if (keyboardRoot == null || !keyboardRoot.activeSelf) return;
            HandleDrag();
        }

        // ── Drag logic ─────────────────────────────────────────────────────────

        private void HandleDrag()
        {
            // Check both hands for grip press
            bool anyGrip = false;
            foreach (XRNode node in new[] { XRNode.LeftHand, XRNode.RightHand })
            {
                InputDevices.GetDevicesAtXRNode(node, _deviceBuf);
                if (_deviceBuf.Count == 0) continue;
                var device = _deviceBuf[0];

                device.TryGetFeatureValue(CommonUsages.gripButton, out bool grip);
                if (!grip) continue;

                anyGrip      = true;
                draggingNode = node;

                // World-space position + forward direction of the controller
                device.TryGetFeatureValue(CommonUsages.devicePosition,  out Vector3    ctrlPos);
                device.TryGetFeatureValue(CommonUsages.deviceRotation,  out Quaternion ctrlRot);
                device.TryGetFeatureValue(CommonUsages.primary2DAxis,   out Vector2    stick);

                // Convert tracking-space → world-space via XROrigin offset
                Transform origin = FindXROrigin();
                if (origin != null)
                {
                    ctrlPos = origin.TransformPoint(ctrlPos);
                    ctrlRot = origin.rotation * ctrlRot;
                }

                // Thumbstick Y → push / pull distance (0.3 m … 2.0 m)
                dragDistance = Mathf.Clamp(dragDistance + stick.y * Time.deltaTime * 0.8f, 0.3f, 2.0f);

                // Thumbstick X → yaw the keyboard left / right
                float yawDelta = stick.x * Time.deltaTime * 60f;  // degrees/sec

                // Target position: straight ahead of the controller at dragDistance
                Vector3 target = ctrlPos + ctrlRot * Vector3.forward * dragDistance;

                // Face the camera
                Camera cam = Camera.main;
                Vector3 lookDir = cam != null ? (target - cam.transform.position).normalized : ctrlRot * Vector3.forward;
                Quaternion faceRot = Quaternion.LookRotation(lookDir, Vector3.up);
                faceRot = Quaternion.Euler(faceRot.eulerAngles.x, faceRot.eulerAngles.y + yawDelta, 0f);

                // Smooth lerp
                keyboardRoot.transform.position = Vector3.Lerp(
                    keyboardRoot.transform.position, target, Time.deltaTime * 12f);
                keyboardRoot.transform.rotation = Quaternion.Slerp(
                    keyboardRoot.transform.rotation, faceRot, Time.deltaTime * 12f);

                break; // first gripping hand wins
            }
        }

        private Transform _cachedOrigin;
        private Transform FindXROrigin()
        {
            if (_cachedOrigin != null) return _cachedOrigin;
            var originObj = GameObject.Find("XR Origin");
            if (originObj != null) _cachedOrigin = originObj.transform;
            return _cachedOrigin;
        }

        // ── Public API ─────────────────────────────────────────────────────────

        /// <summary>Opens the keyboard and targets a TMP_InputField.</summary>
        public void Open(TMP_InputField field)
        {
            targetTMP    = field;
            targetLegacy = null;
            isPassword   = field != null && field.contentType == TMP_InputField.ContentType.Password;
            buffer       = field != null ? field.text : "";
            capsOn       = false;
            RefreshDisplay();
            SetVisible(true);
            PositionInFrontOfCamera();
        }

        /// <summary>Opens the keyboard and targets a legacy InputField.</summary>
        public void Open(InputField field)
        {
            targetLegacy = field;
            targetTMP    = null;
            isPassword   = field != null && field.contentType == InputField.ContentType.Password;
            buffer       = field != null ? field.text : "";
            capsOn       = false;
            RefreshDisplay();
            SetVisible(true);
            PositionInFrontOfCamera();
        }

        public void Close()
        {
            SetVisible(false);
            CommitToField();
            targetTMP    = null;
            targetLegacy = null;
        }

        // ── Key handlers ───────────────────────────────────────────────────────

        private void OnKeyPressed(string value)
        {
            buffer += value;
            RefreshDisplay();
        }

        private void OnBackspace()
        {
            if (buffer.Length > 0)
                buffer = buffer.Substring(0, buffer.Length - 1);
            RefreshDisplay();
        }

        private void OnSpace()
        {
            buffer += " ";
            RefreshDisplay();
        }

        private void OnCaps()
        {
            capsOn = !capsOn;
            RebuildRows();
        }

        private void OnDone()
        {
            CommitToField();
            SetVisible(false);
        }

        private void OnClear()
        {
            buffer = "";
            RefreshDisplay();
        }

        // ── Helpers ────────────────────────────────────────────────────────────

        private void CommitToField()
        {
            if (targetTMP    != null) { targetTMP.text    = buffer; targetTMP.onEndEdit.Invoke(buffer); }
            if (targetLegacy != null) { targetLegacy.text = buffer; targetLegacy.onEndEdit.Invoke(buffer); }
        }

        private void RefreshDisplay()
        {
            if (displayText == null) return;
            if (isPassword)
                displayText.text = new string('●', buffer.Length);
            else
                displayText.text = buffer.Length == 0 ? "<color=#aaa>Type here...</color>" : buffer;
            // also push live text to the field
            if (targetTMP    != null) targetTMP.text    = buffer;
            if (targetLegacy != null) targetLegacy.text = buffer;
        }

        private void SetVisible(bool v) { if (keyboardRoot != null) keyboardRoot.SetActive(v); }

        private void PositionInFrontOfCamera()
        {
            Camera cam = Camera.main;
            if (cam == null || keyboardRoot == null) return;
            // 0.65 m in front, 0.25 m below eye level
            Vector3 pos = cam.transform.position
                        + cam.transform.forward * 0.65f
                        + Vector3.down * 0.25f;
            keyboardRoot.transform.position = pos;
            keyboardRoot.transform.rotation = Quaternion.LookRotation(pos - cam.transform.position, Vector3.up);
        }

        // ── Build the keyboard UI entirely in code ─────────────────────────────

        private void BuildKeyboard()
        {
            // Root world-space canvas
            keyboardRoot = new GameObject("VRKeyboard_Root");
            keyboardRoot.transform.SetParent(transform, false);

            var canvas = keyboardRoot.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            var cr = canvas.GetComponent<RectTransform>();
            cr.sizeDelta = new Vector2(600, 340);
            keyboardRoot.transform.localScale = Vector3.one * 0.0008f;

            keyboardRoot.AddComponent<CanvasScaler>();

            // Add TrackedDeviceGraphicRaycaster so XR ray can hit it
            if (keyboardRoot.GetComponent<GraphicRaycaster>() == null)
                keyboardRoot.AddComponent<GraphicRaycaster>();
            if (keyboardRoot.GetComponent<TrackedDeviceGraphicRaycaster>() == null)
                keyboardRoot.AddComponent<TrackedDeviceGraphicRaycaster>();

            // Background panel
            var bg = CreatePanel(keyboardRoot.transform, "BG", new Color(0.1f, 0.1f, 0.15f, 0.97f));
            var bgRect = bg.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = bgRect.offsetMax = Vector2.zero;

            // Text display bar
            var bar = CreatePanel(bg.transform, "Bar", new Color(0.05f, 0.05f, 0.1f, 1f));
            var barRect = bar.GetComponent<RectTransform>();
            barRect.anchorMin = new Vector2(0.02f, 0.80f);
            barRect.anchorMax = new Vector2(0.98f, 0.97f);
            barRect.offsetMin = barRect.offsetMax = Vector2.zero;

            displayText = CreateTMP(bar.transform, "Display", "", 18, new Color(0.9f, 0.9f, 1f));
            var dtRect  = displayText.GetComponent<RectTransform>();
            dtRect.anchorMin = Vector2.zero; dtRect.anchorMax = new Vector2(0.72f, 1f);
            dtRect.offsetMin = new Vector2(8, 2); dtRect.offsetMax = new Vector2(-4, -2);
            displayText.alignment = TextAlignmentOptions.MidlineLeft;

            // Drag hint — shown in the top-right of the bar
            var hint = CreateTMP(bar.transform, "DragHint", "✥ Grip+Stick to move", 9, new Color(0.5f, 0.7f, 1f, 0.8f));
            var hintRect = hint.GetComponent<RectTransform>();
            hintRect.anchorMin = new Vector2(0.73f, 0f); hintRect.anchorMax = Vector2.one;
            hintRect.offsetMin = new Vector2(2, 2);      hintRect.offsetMax = new Vector2(-6, -2);
            hint.alignment = TextAlignmentOptions.MidlineRight;
            hint.enableWordWrapping = false;

            // Key area
            float startY   = 0.76f;
            float rowH     = 0.17f;

            // Number / top row
            BuildRow(bg.transform, RowsNormal[0], RowsShifted[0], 0, startY, startY - rowH);
            // QWERTY
            BuildRow(bg.transform, RowsNormal[1], RowsShifted[1], 1, startY - rowH, startY - 2 * rowH);
            // ASDF
            BuildRow(bg.transform, RowsNormal[2], RowsShifted[2], 2, startY - 2 * rowH, startY - 3 * rowH);
            // ZXCV + Backspace
            BuildRow(bg.transform, RowsNormal[3], RowsShifted[3], 3, startY - 3 * rowH, startY - 4 * rowH);

            // Bottom function row
            float bY0 = startY - 4 * rowH;
            float bY1 = bY0 - rowH;

            MakeSpecialButton(bg.transform, "CAPS",  new Vector2(0.01f, bY1), new Vector2(0.16f, bY0), OnCaps,    new Color(0.3f,0.3f,0.5f));
            MakeSpecialButton(bg.transform, "SPACE", new Vector2(0.17f, bY1), new Vector2(0.63f, bY0), OnSpace,   new Color(0.2f,0.2f,0.3f));
            MakeSpecialButton(bg.transform, "⌫",     new Vector2(0.64f, bY1), new Vector2(0.79f, bY0), OnBackspace, new Color(0.4f,0.15f,0.15f));
            MakeSpecialButton(bg.transform, "✓ Done",new Vector2(0.80f, bY1), new Vector2(0.99f, bY0), OnDone,    new Color(0.1f,0.45f,0.2f));
        }

        // ── Row builder ────────────────────────────────────────────────────────

        private readonly List<(Button btn, TextMeshProUGUI label, string normal, string shifted)> keyButtons
            = new List<(Button, TextMeshProUGUI, string, string)>();

        private void BuildRow(Transform parent, string[] normal, string[] shifted, int rowIdx, float yMax, float yMin)
        {
            int count  = normal.Length;
            float padX = 0.01f;
            float w    = (1f - padX * (count + 1)) / count;

            for (int i = 0; i < count; i++)
            {
                float xMin = padX + i * (w + padX);
                float xMax = xMin + w;

                string normKey    = normal[i];
                string shiftedKey = shifted[i];

                var btn   = MakeKeyButton(parent, normKey, new Vector2(xMin, yMin), new Vector2(xMax, yMax));
                var label = btn.GetComponentInChildren<TextMeshProUGUI>();

                keyButtons.Add((btn, label, normKey, shiftedKey));

                btn.onClick.AddListener(() => OnKeyPressed(capsOn ? shiftedKey : normKey));
            }
        }

        private void RebuildRows()
        {
            foreach (var entry in keyButtons)
            {
                if (entry.label != null)
                    entry.label.text = capsOn ? entry.shifted : entry.normal;
            }
        }

        // ── Widget factories ───────────────────────────────────────────────────

        private GameObject CreatePanel(Transform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = color;
            return go;
        }

        private TextMeshProUGUI CreateTMP(Transform parent, string name, string text, float size, Color color)
        {
            var go  = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.text      = text;
            tmp.fontSize  = size;
            tmp.color     = color;
            tmp.enableWordWrapping = false;
            tmp.overflowMode = TextOverflowModes.Ellipsis;
            return tmp;
        }

        private Button MakeKeyButton(Transform parent, string label, Vector2 anchorMin, Vector2 anchorMax)
        {
            var go   = new GameObject("Key_" + label, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);

            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin; rect.anchorMax = anchorMax;
            rect.offsetMin = rect.offsetMax = Vector2.zero;

            var img  = go.GetComponent<Image>();
            img.color = new Color(0.25f, 0.25f, 0.38f, 1f);

            var btn  = go.GetComponent<Button>();
            var cb   = btn.colors;
            cb.normalColor      = new Color(0.25f, 0.25f, 0.38f, 1f);
            cb.highlightedColor = new Color(0.45f, 0.45f, 0.7f, 1f);
            cb.pressedColor     = new Color(0.6f, 0.6f, 0.9f, 1f);
            btn.colors = cb;

            var lbl = CreateTMP(go.transform, "Label", label, 16, Color.white);
            var lr  = lbl.GetComponent<RectTransform>();
            lr.anchorMin = Vector2.zero; lr.anchorMax = Vector2.one;
            lr.offsetMin = lr.offsetMax = Vector2.zero;
            lbl.alignment = TextAlignmentOptions.Center;

            return btn;
        }

        private void MakeSpecialButton(Transform parent, string label, Vector2 anchorMin, Vector2 anchorMax, Action onClick, Color color)
        {
            var go   = new GameObject("Btn_" + label, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);

            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin; rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.one * 2; rect.offsetMax = Vector2.one * -2;

            var img  = go.GetComponent<Image>();
            img.color = color;

            var btn  = go.GetComponent<Button>();
            var cb   = btn.colors;
            cb.highlightedColor = color * 1.4f;
            cb.pressedColor     = color * 1.7f;
            btn.colors = cb;
            btn.onClick.AddListener(() => onClick());

            var lbl = CreateTMP(go.transform, "Label", label, 14, Color.white);
            var lr  = lbl.GetComponent<RectTransform>();
            lr.anchorMin = Vector2.zero; lr.anchorMax = Vector2.one;
            lr.offsetMin = lr.offsetMax = Vector2.zero;
            lbl.alignment = TextAlignmentOptions.Center;
        }

        // ── Ensure singleton exists ────────────────────────────────────────────
        public static VRKeyboard EnsureInstance()
        {
            if (Instance != null) return Instance;
            var go = new GameObject("VRKeyboard");
            DontDestroyOnLoad(go);
            return go.AddComponent<VRKeyboard>();
        }
    }
}
