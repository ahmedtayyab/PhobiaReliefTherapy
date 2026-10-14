using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;

namespace PhobiaReliefTherapy.VR
{
    /// <summary>
    /// Grip-grabs only the canvas the laser actually hits. Nearest collider wins so
    /// graphs and menus never move together.
    /// </summary>
    [RequireComponent(typeof(Canvas))]
    public class VRMenuDragger : MonoBehaviour
    {
        public bool UserPlaced { get; private set; }
        public bool IsVitalsHud;

        public static event System.Action<VRMenuDragger> GrabStarted;

        private static int claimFrame = -1;
        private static float bestHitDistance = float.MaxValue;
        private static VRMenuDragger bestCandidate;
        private static VRMenuDragger activeDrag;

        private BoxCollider canvasCollider;
        private bool isDragging;
        private XRNode draggingHand = XRNode.RightHand;
        private float dragDistance = 1.45f;

        private void OnEnable()
        {
            RefreshCollider();
        }

        private void Start()
        {
            RefreshCollider();
        }

        private void OnDisable()
        {
            if (activeDrag == this)
                StopDrag();
        }

        private void OnDestroy()
        {
            if (activeDrag == this)
                activeDrag = null;
            if (bestCandidate == this)
                bestCandidate = null;
        }

        public static void ReleaseAllGrabs()
        {
            if (activeDrag != null)
                activeDrag.StopDrag();
            activeDrag = null;
            bestCandidate = null;
            bestHitDistance = float.MaxValue;
            claimFrame = -1;
        }

        public void ReleaseGrab()
        {
            if (activeDrag == this || isDragging)
                StopDrag();
        }

        private void Update()
        {
            if (activeDrag != null &&
                (!activeDrag.isActiveAndEnabled || !activeDrag.gameObject.activeInHierarchy))
            {
                activeDrag = null;
            }

            if (isDragging)
            {
                if (activeDrag != this || !IsGripDown(draggingHand) || !TryGetAim(draggingHand, out Ray aim, out Vector2 stick))
                {
                    StopDrag();
                    return;
                }

                dragDistance = Mathf.Clamp(dragDistance + stick.y * Time.deltaTime * 1.1f, 0.45f, 3.2f);
                float scale = transform.localScale.x;
                scale = Mathf.Clamp(scale * (1f + stick.x * Time.deltaTime * 0.9f), 0.00032f, 0.0015f);
                transform.localScale = Vector3.one * scale;

                Vector3 targetPosition = aim.origin + aim.direction * dragDistance;
                Camera cam = Camera.main;
                Vector3 lookDir = cam != null
                    ? (targetPosition - cam.transform.position).normalized
                    : aim.direction;

                transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * 14f);
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDir, Vector3.up), Time.deltaTime * 14f);
                RememberPose();
                return;
            }

            if (activeDrag != null)
                return;

            ResetClaimIfNewFrame();

            foreach (XRNode hand in new[] { XRNode.RightHand, XRNode.LeftHand })
            {
                if (!IsGripDown(hand) || !TryGetAim(hand, out Ray aim, out _))
                    continue;

                RefreshCollider();
                if (canvasCollider != null && canvasCollider.Raycast(aim, out RaycastHit hit, 8f) && hit.distance < bestHitDistance)
                {
                    bestHitDistance = hit.distance;
                    bestCandidate = this;
                    draggingHand = hand;
                }
            }
        }

        private void LateUpdate()
        {
            if (activeDrag != null || bestCandidate != this)
                return;

            BeginDrag(draggingHand, bestHitDistance);
        }

        public void RefreshCollider()
        {
            RectTransform rect = GetComponent<RectTransform>();
            canvasCollider = GetComponent<BoxCollider>();
            if (canvasCollider == null)
                canvasCollider = gameObject.AddComponent<BoxCollider>();

            float width = Mathf.Max(120f, rect.rect.width);
            float height = Mathf.Max(120f, rect.rect.height);
            canvasCollider.size = new Vector3(width, height, 48f);
            canvasCollider.center = new Vector3(
                rect.rect.x + rect.rect.width * 0.5f,
                rect.rect.y + rect.rect.height * 0.5f,
                0f);
            canvasCollider.isTrigger = true;
        }

        private void BeginDrag(XRNode hand, float distance)
        {
            PrepareForWorldDrag();
            isDragging = true;
            UserPlaced = true;
            activeDrag = this;
            draggingHand = hand;
            dragDistance = Mathf.Clamp(distance, 0.45f, 3.2f);
            GrabStarted?.Invoke(this);
        }

        private void StopDrag()
        {
            RememberPose();
            isDragging = false;
            if (activeDrag == this)
                activeDrag = null;
        }

        private void RememberPose()
        {
            if (IsVitalsHud)
                VrMenuPose.CaptureHud(transform);
            else
                VrMenuPose.CaptureScreen(transform);
        }

        private void PrepareForWorldDrag()
        {
            var canvas = GetComponent<Canvas>();
            Camera cam = VrWorldMenu.FindVrCamera();
            if (canvas == null || cam == null)
                return;

            if (canvas.renderMode != RenderMode.WorldSpace)
                VrWorldMenu.AttachAndPlace(canvas, cam, recenterIfLoose: false);

            RefreshCollider();
        }

        private static void ResetClaimIfNewFrame()
        {
            if (claimFrame == Time.frameCount)
                return;

            claimFrame = Time.frameCount;
            bestHitDistance = float.MaxValue;
            bestCandidate = null;
        }

        public static bool TryGetPointerAim(XRNode hand, out Ray aim, out Vector2 stick)
        {
            return TryGetAim(hand, out aim, out stick);
        }

        private static XRRayInteractor[] cachedInteractors;
        private static float interactorCacheTime;

        private static XRRayInteractor[] Interactors()
        {
            if (cachedInteractors == null || Time.unscaledTime - interactorCacheTime > 0.4f)
            {
                cachedInteractors = Object.FindObjectsOfType<XRRayInteractor>();
                interactorCacheTime = Time.unscaledTime;
            }

            return cachedInteractors;
        }

        private static bool TryGetAim(XRNode hand, out Ray aim, out Vector2 stick)
        {
            aim = default;
            stick = Vector2.zero;

            var interactors = Interactors();
            if (interactors != null)
            {
                for (int i = 0; i < interactors.Length; i++)
                {
                    var interactor = interactors[i];
                    if (interactor == null)
                        continue;
                    var controller = interactor.GetComponent<XRController>();
                    if (controller == null || controller.controllerNode != hand)
                        continue;
                    if (interactor.rayOriginTransform == null)
                        continue;

                    aim = new Ray(interactor.rayOriginTransform.position, interactor.rayOriginTransform.forward);
                    InputDevice device = InputDevices.GetDeviceAtXRNode(hand);
                    if (device.isValid)
                        device.TryGetFeatureValue(CommonUsages.primary2DAxis, out stick);
                    return true;
                }
            }

            InputDevice fallback = InputDevices.GetDeviceAtXRNode(hand);
            if (!fallback.isValid)
                return false;

            fallback.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 pos);
            fallback.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion rot);
            fallback.TryGetFeatureValue(CommonUsages.primary2DAxis, out stick);
            Camera cam = Camera.main;
            if (cam != null)
            {
                pos = cam.transform.position + cam.transform.rotation * pos;
                rot = cam.transform.rotation * rot;
            }

            aim = new Ray(pos, rot * Vector3.forward);
            return true;
        }

        private static bool IsGripDown(XRNode hand)
        {
            InputDevice device = InputDevices.GetDeviceAtXRNode(hand);
            if (!device.isValid)
                return false;
            if (device.TryGetFeatureValue(CommonUsages.gripButton, out bool gripButton) && gripButton)
                return true;
            return device.TryGetFeatureValue(CommonUsages.grip, out float grip) && grip >= 0.65f;
        }

        public static bool AnyGrabActive => activeDrag != null;
    }
}
