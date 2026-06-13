using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.XR;
using System.Collections.Generic;

namespace PhobiaReliefTherapy.VR
{
    /// <summary>
    /// Manages VR UI interaction. Draws a controller-based laser pointer if a VR controller is tracking,
    /// and falls back to a head-locked gaze pointer (dwell selection) if controllers are inactive.
    /// </summary>
    public class VRGazePointer : MonoBehaviour
    {
        private GameObject reticleCanvas;
        private Image reticleImage;
        private LineRenderer lineRenderer;
        
        private Button hoveredButton;
        private float hoverTimer = 0f;
        private const float DWELL_TIME = 1.5f;

        private Vector3 controllerWorldPos;
        private Vector3 controllerWorldDir;
        private bool isControllerActive = false;

        private void Start()
        {
            CreateReticleAndLine();
        }

        private void OnDestroy()
        {
            if (reticleCanvas != null) Destroy(reticleCanvas);
            if (lineRenderer != null) Destroy(lineRenderer.gameObject);
        }

        private void CreateReticleAndLine()
        {
            // Create a small Canvas for the reticle dot
            reticleCanvas = new GameObject("VRPointerReticleCanvas", typeof(Canvas), typeof(CanvasScaler));
            reticleCanvas.transform.SetParent(this.transform, false);

            Canvas canvas = reticleCanvas.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = this.GetComponent<Camera>() ?? Camera.main;
            canvas.planeDistance = 0.95f; // Place it slightly closer than the UI canvases (1.2f)
            canvas.sortingOrder = 10000;

            CanvasScaler scaler = reticleCanvas.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            GameObject reticleDot = new GameObject("ReticleDot", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            reticleDot.transform.SetParent(reticleCanvas.transform, false);

            RectTransform rect = reticleDot.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(16f, 16f);
            rect.anchoredPosition = Vector2.zero;

            reticleImage = reticleDot.GetComponent<Image>();
            reticleImage.color = Color.white;

            // Load a circle sprite if available
            Sprite circleSprite = Resources.Load<Sprite>("reticle_circle");
            if (circleSprite != null)
            {
                reticleImage.sprite = circleSprite;
            }

            // Create a LineRenderer for the controller laser pointer
            GameObject lineGO = new GameObject("ControllerLaserLine", typeof(LineRenderer));
            lineGO.transform.SetParent(this.transform, false);
            
            lineRenderer = lineGO.GetComponent<LineRenderer>();
            lineRenderer.startWidth = 0.01f;
            lineRenderer.endWidth = 0.002f;
            lineRenderer.useWorldSpace = true;
            
            // Set a simple default UI sprite material for the line
            Shader defaultShader = Shader.Find("Sprites/Default") ?? Shader.Find("Legacy Shaders/Particles/Alpha Blended Premultiply");
            if (defaultShader != null)
            {
                lineRenderer.material = new Material(defaultShader);
            }
            lineRenderer.startColor = Color.cyan;
            lineRenderer.endColor = new Color(0.0f, 1.0f, 1.0f, 0.1f);
            lineRenderer.positionCount = 2;
            lineRenderer.enabled = false;
        }

        private void Update()
        {
            if (EventSystem.current == null || reticleImage == null)
                return;

            UpdateControllerState();

            Vector2 screenPoint = new Vector2(Screen.width / 2f, Screen.height / 2f);
            bool hasHit = false;

            if (isControllerActive)
            {
                // Find the main UI Canvas in the scene
                Canvas targetCanvas = FindObjectOfType<Canvas>();
                if (targetCanvas != null)
                {
                    Plane canvasPlane = new Plane(-targetCanvas.transform.forward, targetCanvas.transform.position);
                    Ray ray = new Ray(controllerWorldPos, controllerWorldDir);
                    
                    if (canvasPlane.Raycast(ray, out float enter))
                    {
                        Vector3 hitPoint = ray.GetPoint(enter);
                        screenPoint = Camera.main.WorldToScreenPoint(hitPoint);
                        
                        // Check if the raycast intersection lands within the viewport bounds
                        if (screenPoint.x >= 0 && screenPoint.x <= Screen.width && screenPoint.y >= 0 && screenPoint.y <= Screen.height)
                        {
                            hasHit = true;
                            // Position the reticle image directly at the 3D world position where the laser hits
                            reticleImage.rectTransform.position = hitPoint;
                            
                            // Render the controller laser pointer line
                            lineRenderer.enabled = true;
                            lineRenderer.SetPosition(0, controllerWorldPos);
                            lineRenderer.SetPosition(1, hitPoint);
                        }
                    }
                }
            }

            if (!hasHit)
            {
                // Fallback: Head-locked Gaze Reticle at center screen
                screenPoint = new Vector2(Screen.width / 2f, Screen.height / 2f);
                reticleImage.rectTransform.anchoredPosition = Vector2.zero; // Reset to camera center
                lineRenderer.enabled = false;
            }

            // Perform standard UI Raycast using the computed screenPoint (from laser hit or head center)
            PointerEventData pointerData = new PointerEventData(EventSystem.current)
            {
                position = screenPoint
            };

            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointerData, results);

            Button foundButton = null;
            foreach (var result in results)
            {
                Button btn = result.gameObject.GetComponentInParent<Button>();
                if (btn != null && btn.interactable)
                {
                    foundButton = btn;
                    break;
                }
            }

            if (foundButton != null)
            {
                if (hoveredButton != foundButton)
                {
                    hoveredButton = foundButton;
                    hoverTimer = 0f;
                    reticleImage.color = Color.cyan;
                }

                hoverTimer += Time.deltaTime;
                bool clicked = false;
                
                if (isControllerActive)
                {
                    // Check physical trigger press on the right controller
                    var devices = new List<InputDevice>();
                    InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.Right | InputDeviceCharacteristics.Controller, devices);
                    if (devices.Count > 0)
                    {
                        InputDevice device = devices[0];
                        if (device.isValid)
                        {
                            if (device.TryGetFeatureValue(CommonUsages.triggerButton, out bool triggerPressed) && triggerPressed)
                            {
                                clicked = true;
                            }
                            else if (device.TryGetFeatureValue(CommonUsages.trigger, out float triggerAxis) && triggerAxis > 0.5f)
                            {
                                clicked = true;
                            }
                        }
                    }
                    
                    // Fallback to primary mouse button/JoystickButton0
                    if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.JoystickButton0))
                    {
                        clicked = true;
                    }
                }
                else
                {
                    // Gaze Dwell Auto-Click progress
                    float progress = Mathf.Clamp01(hoverTimer / DWELL_TIME);
                    reticleImage.rectTransform.sizeDelta = Vector2.Lerp(new Vector2(16f, 16f), new Vector2(6f, 6f), progress);
                    
                    if (hoverTimer >= DWELL_TIME || Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.JoystickButton0) || Input.GetKeyDown(KeyCode.Space))
                    {
                        clicked = true;
                    }
                }

                if (clicked)
                {
                    Debug.Log($"[VRGazePointer] Triggering click on Button: {hoveredButton.name}");
                    
                    // Trigger the UI button's submit handler
                    ExecuteEvents.Execute(hoveredButton.gameObject, new PointerEventData(EventSystem.current), ExecuteEvents.submitHandler);
                    
                    hoveredButton = null;
                    hoverTimer = 0f;
                    reticleImage.color = Color.white;
                    reticleImage.rectTransform.sizeDelta = new Vector2(16f, 16f);
                }
            }
            else
            {
                if (hoveredButton != null)
                {
                    hoveredButton = null;
                    hoverTimer = 0f;
                    reticleImage.color = Color.white;
                    reticleImage.rectTransform.sizeDelta = new Vector2(16f, 16f);
                }
            }
        }

        private void UpdateControllerState()
        {
            isControllerActive = false;
            
            var devices = new List<InputDevice>();
            InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.Right | InputDeviceCharacteristics.Controller, devices);
            
            if (devices.Count > 0)
            {
                InputDevice device = devices[0];
                if (device.isValid)
                {
                    bool hasPos = device.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 localPos);
                    bool hasRot = device.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion localRot);
                    
                    if (hasPos && hasRot)
                    {
                        isControllerActive = true;
                        Transform parent = Camera.main.transform.parent;
                        
                        // Transform to world space if the camera has an offset rig parent
                        if (parent != null)
                        {
                            controllerWorldPos = parent.TransformPoint(localPos);
                            controllerWorldDir = parent.TransformDirection(localRot * Vector3.forward);
                        }
                        else
                        {
                            controllerWorldPos = localPos;
                            controllerWorldDir = localRot * Vector3.forward;
                        }
                    }
                }
            }
        }
    }
}
