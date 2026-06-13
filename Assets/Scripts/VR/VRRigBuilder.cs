using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Unity.XR.CoreUtils;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace PhobiaReliefTherapy.VR
{
    /// <summary>
    /// Dynamically constructs a native XR Interaction Toolkit VR rig (XR Origin, controllers,
    /// laser pointers, and XR UI event systems) at runtime to support physical controller input.
    /// </summary>
    public static class VRRigBuilder
    {
        public static void BuildVRRig(Camera mainCamera, Canvas uiCanvas)
        {
            if (mainCamera == null) return;

            // Check if an XR Origin is already present in the scene
            if (Object.FindObjectOfType<XROrigin>() != null)
            {
                Debug.Log("[VRRigBuilder] XR Origin already exists in the scene.");
                return;
            }

            Debug.Log("[VRRigBuilder] Dynamically building native XR Interaction Rig...");

            // 1. Create XR Interaction Manager
            XRInteractionManager interactionManager = Object.FindObjectOfType<XRInteractionManager>();
            if (interactionManager == null)
            {
                GameObject managerGO = new GameObject("XR Interaction Manager", typeof(XRInteractionManager));
                interactionManager = managerGO.GetComponent<XRInteractionManager>();
            }

            // 2. Create XR Origin Root
            GameObject originGO = new GameObject("XR Origin", typeof(XROrigin));
            XROrigin origin = originGO.GetComponent<XROrigin>();

            // Align the VR Rig to the camera's X and Z position, but place it on the floor (Y = 0)
            float initialCameraHeight = mainCamera.transform.position.y;
            originGO.transform.position = new Vector3(mainCamera.transform.position.x, 0f, mainCamera.transform.position.z);
            originGO.transform.rotation = Quaternion.Euler(0f, mainCamera.transform.rotation.eulerAngles.y, 0f);

            // Create Camera Offset parent
            GameObject offsetGO = new GameObject("Camera Offset");
            offsetGO.transform.SetParent(originGO.transform, false);
            origin.CameraFloorOffsetObject = offsetGO;

            // Reparent Main Camera relative to the VR Rig
            mainCamera.transform.SetParent(offsetGO.transform, false);
            // Position the camera at its initial height relative to the floor offset object
            mainCamera.transform.localPosition = new Vector3(0f, initialCameraHeight, 0f);
            mainCamera.transform.localRotation = Quaternion.identity;
            origin.Camera = mainCamera;

            // Add TrackedPoseDriver to the main camera to enable headset tracking (head rotation and translation)
            var trackedPoseDriver = mainCamera.gameObject.GetComponent<UnityEngine.SpatialTracking.TrackedPoseDriver>();
            if (trackedPoseDriver == null)
            {
                trackedPoseDriver = mainCamera.gameObject.AddComponent<UnityEngine.SpatialTracking.TrackedPoseDriver>();
            }
            trackedPoseDriver.SetPoseSource(
                UnityEngine.SpatialTracking.TrackedPoseDriver.DeviceType.GenericXRDevice,
                UnityEngine.SpatialTracking.TrackedPoseDriver.TrackedPose.Center
            );
            
            // Set default Camera Y Offset for non-tracked/Device-based fallback environments
            origin.CameraYOffset = initialCameraHeight;
            origin.RequestedTrackingOriginMode = XROrigin.TrackingOriginMode.Floor;

            // 3. Create Left and Right Hand Controllers with Raycast Visuals and Comfort Offset
            CreateController(offsetGO.transform, "Left Hand Controller",  XRNode.LeftHand,  interactionManager, mainCamera);
            CreateController(offsetGO.transform, "Right Hand Controller", XRNode.RightHand, interactionManager, mainCamera);

            // 4. Configure Canvas for tracked controller raycasting
            if (uiCanvas != null)
            {
                // Remove standard GraphicRaycaster
                var oldRaycaster = uiCanvas.GetComponent<GraphicRaycaster>();
                if (oldRaycaster != null)
                {
                    Object.Destroy(oldRaycaster);
                }

                // Add Tracked Device Graphic Raycaster
                if (uiCanvas.GetComponent<TrackedDeviceGraphicRaycaster>() == null)
                {
                    uiCanvas.gameObject.AddComponent<TrackedDeviceGraphicRaycaster>();
                }
            }

            // 5. Configure EventSystem for XR UI input handling
            EventSystem eventSystem = Object.FindObjectOfType<EventSystem>();
            if (eventSystem != null)
            {
                // Remove old Standalone/InputSystem input modules
                var oldInputModule = eventSystem.GetComponent<BaseInputModule>();
                if (oldInputModule != null)
                {
                    Object.Destroy(oldInputModule);
                }

                // Add native XR UI Input Module
                if (eventSystem.GetComponent<XRUIInputModule>() == null)
                {
                    eventSystem.gameObject.AddComponent<XRUIInputModule>();
                }
            }
        }

        private static void CreateController(Transform parent, string name, XRNode node,
                                             XRInteractionManager interactionManager, Camera mainCamera)
        {
            GameObject controllerGO = new GameObject(name,
                typeof(XRController),
                typeof(XRRayInteractor),
                typeof(LineRenderer),
                typeof(XRInteractorLineVisual)
            );
            controllerGO.transform.SetParent(parent, false);

            // Configure Device-Based Controller tracking
            XRController controller = controllerGO.GetComponent<XRController>();
            controller.controllerNode = node;

            // ── Ray Origin ────────────────────────────────────────────────────────
            // Parent the Ray Origin directly to the controller so aiming is controller-driven.
            // We apply a comfort pointing angle (15 degrees pitch down) so the user does not have
            // to bend their hand or wrist down to aim at the UI panel.
            GameObject rayOriginGO = new GameObject("Ray Origin");
            rayOriginGO.transform.SetParent(controllerGO.transform, false);
            rayOriginGO.transform.localPosition = new Vector3(0f, -0.02f, 0.05f); // slightly forward/down from grip anchor
            rayOriginGO.transform.localRotation = Quaternion.Euler(15f, 0f, 0f);

            XRRayInteractor rayInteractor = controllerGO.GetComponent<XRRayInteractor>();
            rayInteractor.interactionManager  = interactionManager;
            rayInteractor.rayOriginTransform  = rayOriginGO.transform;
            rayInteractor.attachTransform     = rayOriginGO.transform;

            // ── Line Renderer (laser visual) ──────────────────────────────────────
            LineRenderer lineRenderer = controllerGO.GetComponent<LineRenderer>();
            lineRenderer.startWidth    = 0.005f;
            lineRenderer.endWidth      = 0.001f;
            lineRenderer.useWorldSpace = true;

            Shader defaultShader = Shader.Find("Sprites/Default")
                                ?? Shader.Find("Legacy Shaders/Particles/Alpha Blended Premultiply");
            if (defaultShader != null)
                lineRenderer.material = new Material(defaultShader);

            // Configure visual line colors directly
            lineRenderer.startColor = new Color(0f, 0.9f, 1f, 0.8f);
            lineRenderer.endColor   = new Color(0f, 0.9f, 1f, 0.1f);

            // ── Configure XR Interactor Line Visual ──────────────────────────────
            // XRInteractorLineVisual overrides the LineRenderer colors and widths at runtime,
            // so we must configure its gradient directly in code.
            XRInteractorLineVisual lineVisual = controllerGO.GetComponent<XRInteractorLineVisual>();
            if (lineVisual != null)
            {
                Gradient laserGradient = new Gradient();
                laserGradient.SetKeys(
                    new GradientColorKey[] { 
                        new GradientColorKey(new Color(0f, 0.9f, 1f), 0f), 
                        new GradientColorKey(new Color(0f, 0.9f, 1f), 1f) 
                    },
                    new GradientAlphaKey[] { 
                        new GradientAlphaKey(0.8f, 0f), 
                        new GradientAlphaKey(0.1f, 1f) 
                    }
                );

                lineVisual.validColorGradient = laserGradient;
                lineVisual.invalidColorGradient = laserGradient;
                lineVisual.lineWidth = 0.005f;
            }
        }
    }
}
