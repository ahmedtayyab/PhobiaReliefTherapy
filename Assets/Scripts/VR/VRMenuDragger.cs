using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

namespace PhobiaReliefTherapy.VR
{
    /// <summary>
    /// Attach to any world-space Canvas to allow users to grab it with the Grip button
    /// on their VR controller and move/rotate it using the thumbstick.
    /// </summary>
    [RequireComponent(typeof(Canvas))]
    public class VRMenuDragger : MonoBehaviour
    {
        private BoxCollider canvasCollider;
        private bool isDragging = false;
        private XRNode draggingHand = XRNode.RightHand;
        private float dragDistance = 1.5f;
        private float accumulatedYawOffset = 0f;

        private readonly List<InputDevice> _deviceBuf = new List<InputDevice>();
        private Transform _cachedOrigin;

        private void Start()
        {
            // Auto-configure box collider on the world-space canvas so we can raycast it
            RectTransform rect = GetComponent<RectTransform>();
            canvasCollider = GetComponent<BoxCollider>();
            if (canvasCollider == null)
            {
                canvasCollider = gameObject.AddComponent<BoxCollider>();
            }

            // Adjust collider bounds to match canvas RectTransform size
            canvasCollider.size = new Vector3(rect.rect.width, rect.rect.height, 0.05f);
            canvasCollider.center = new Vector3(rect.rect.x + rect.rect.width / 2f, rect.rect.y + rect.rect.height / 2f, 0f);
        }

        private void Update()
        {
            HandleDrag();
        }

        private void HandleDrag()
        {
            if (isDragging)
            {
                // Continue dragging with the selected hand
                InputDevices.GetDevicesAtXRNode(draggingHand, _deviceBuf);
                if (_deviceBuf.Count == 0)
                {
                    isDragging = false;
                    return;
                }

                var device = _deviceBuf[0];
                device.TryGetFeatureValue(CommonUsages.gripButton, out bool gripPressed);

                if (!gripPressed)
                {
                    // User released the grip, stop dragging
                    isDragging = false;
                    return;
                }

                // Dragging in progress: calculate position and rotation
                device.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 ctrlPos);
                device.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion ctrlRot);
                device.TryGetFeatureValue(CommonUsages.primary2DAxis, out Vector2 stick);

                Transform origin = FindXROrigin();
                if (origin != null)
                {
                    ctrlPos = origin.TransformPoint(ctrlPos);
                    ctrlRot = origin.rotation * ctrlRot;
                }

                // Thumbstick Y changes distance (range: 0.5m to 3.0m)
                dragDistance = Mathf.Clamp(dragDistance + stick.y * Time.deltaTime * 0.8f, 0.5f, 3.0f);

                // Thumbstick X adds permanent yaw offset (rotating the menu)
                accumulatedYawOffset += stick.x * Time.deltaTime * 60f;

                // Move menu target in front of controller
                Vector3 targetPosition = ctrlPos + ctrlRot * Vector3.forward * dragDistance;

                // Rotate menu to face player's camera
                Camera cam = Camera.main;
                Vector3 lookDir = cam != null ? (targetPosition - cam.transform.position).normalized : ctrlRot * Vector3.forward;
                Quaternion faceRotation = Quaternion.LookRotation(lookDir, Vector3.up);
                faceRotation = Quaternion.Euler(faceRotation.eulerAngles.x, faceRotation.eulerAngles.y + accumulatedYawOffset, 0f);

                // Apply smooth translation and rotation
                transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * 12f);
                transform.rotation = Quaternion.Slerp(transform.rotation, faceRotation, Time.deltaTime * 12f);
            }
            else
            {
                // Check both hands to initiate dragging
                foreach (XRNode hand in new[] { XRNode.LeftHand, XRNode.RightHand })
                {
                    InputDevices.GetDevicesAtXRNode(hand, _deviceBuf);
                    if (_deviceBuf.Count == 0) continue;

                    var device = _deviceBuf[0];
                    device.TryGetFeatureValue(CommonUsages.gripButton, out bool gripPressed);

                    if (!gripPressed) continue;

                    // Controller tracking position and rotation
                    device.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 ctrlPos);
                    device.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion ctrlRot);

                    Transform origin = FindXROrigin();
                    if (origin != null)
                    {
                        ctrlPos = origin.TransformPoint(ctrlPos);
                        ctrlRot = origin.rotation * ctrlRot;
                    }

                    // Cast a ray from the controller forward direction to see if it points at this menu
                    Ray ray = new Ray(ctrlPos, ctrlRot * Vector3.forward);
                    if (canvasCollider.Raycast(ray, out RaycastHit hit, 10f))
                    {
                        // Ray hit this menu! Start dragging
                        isDragging = true;
                        draggingHand = hand;
                        dragDistance = hit.distance;
                        
                        // Set initial yaw angle relative to camera view
                        Camera cam = Camera.main;
                        Vector3 lookDir = cam != null ? (transform.position - cam.transform.position).normalized : ctrlRot * Vector3.forward;
                        Quaternion faceRotation = Quaternion.LookRotation(lookDir, Vector3.up);
                        accumulatedYawOffset = transform.rotation.eulerAngles.y - faceRotation.eulerAngles.y;

                        break; // Hand locked
                    }
                }
            }
        }

        private Transform FindXROrigin()
        {
            if (_cachedOrigin != null) return _cachedOrigin;
            var originObj = GameObject.Find("XR Origin");
            if (originObj != null) _cachedOrigin = originObj.transform;
            return _cachedOrigin;
        }
    }
}
