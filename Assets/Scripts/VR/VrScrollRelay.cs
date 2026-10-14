using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;
using UnityEngine.InputSystem;
using TMPro;

namespace PhobiaReliefTherapy.VR
{
    /// <summary>
    /// Scrolls lists in VR with thumbstick while looking at the card, or by
    /// dragging the orange handle with the laser. Mouse wheel still works.
    /// </summary>
    [DefaultExecutionOrder(-80)]
    [RequireComponent(typeof(ScrollRect))]
    public class VrScrollRelay : MonoBehaviour
    {
        private static int locomotionBlockFrame = -1;
        public static bool BlockLocomotion => locomotionBlockFrame == Time.frameCount;

        private static InputAction rightStickAction;
        private static InputAction leftStickAction;

        private ScrollRect scroll;
        private RectTransform rect;
        private Scrollbar bar;

        private void Awake()
        {
            scroll = GetComponent<ScrollRect>();
            rect = GetComponent<RectTransform>();
            bar = scroll != null ? scroll.verticalScrollbar : GetComponentInChildren<Scrollbar>(true);
            EnsureStickActions();
        }

        public void MarkDirty()
        {
            RebuildLayout();
        }

        private void Update()
        {
            if (scroll == null || !scroll.vertical || scroll.content == null)
                return;

            if (scroll.content.rect.height <= scroll.viewport.rect.height + 2f)
                RebuildLayout();

            if (!CanScroll())
                return;

            float delta = Input.mouseScrollDelta.y * 0.14f;
            bool grabbing = VRMenuDragger.AnyGrabActive;
            bool onList = LaserHitsThis(out XRNode hand, out Vector3 hitPoint);
            bool onBar = LaserHitsBar(out XRNode barHand, out Vector3 barPoint);
            bool looking = IsLookingAtList();

            if (!grabbing && TryAnyStickY(out float stickY) && (onList || onBar || looking))
            {
                delta -= stickY * Time.deltaTime * 1.35f;
                locomotionBlockFrame = Time.frameCount;
            }

            if (!grabbing && onBar && IsTriggerDown(barHand))
            {
                SetFromBarPoint(barPoint);
                locomotionBlockFrame = Time.frameCount;
                return;
            }

            if (Mathf.Abs(delta) < 0.00005f)
                return;

            scroll.StopMovement();
            scroll.verticalNormalizedPosition = Mathf.Clamp01(scroll.verticalNormalizedPosition + delta);
        }

        private bool CanScroll()
        {
            if (scroll.viewport == null)
                return false;
            return scroll.content.rect.height > scroll.viewport.rect.height + 1f;
        }

        private void SetFromBarPoint(Vector3 worldPoint)
        {
            if (bar == null)
                return;

            var barRect = bar.transform as RectTransform;
            Vector3 local = barRect.InverseTransformPoint(worldPoint);
            float h = Mathf.Max(1f, barRect.rect.height);
            float t = Mathf.InverseLerp(barRect.rect.yMin, barRect.rect.yMax, local.y);
            scroll.StopMovement();
            scroll.verticalNormalizedPosition = Mathf.Clamp01(t);
        }

        private void RebuildLayout()
        {
            if (scroll == null || scroll.content == null)
                return;

            var body = scroll.content.GetComponentInChildren<TextMeshProUGUI>();
            if (body == null)
                return;

            var fitter = scroll.content.GetComponent<ContentSizeFitter>();
            if (fitter != null)
                fitter.enabled = false;

            Canvas.ForceUpdateCanvases();
            body.enableWordWrapping = true;
            body.ForceMeshUpdate();
            float width = body.rectTransform.rect.width;
            if (width < 8f && scroll.viewport != null)
                width = scroll.viewport.rect.width - 20f;
            width = Mathf.Max(64f, width);
            float height = Mathf.Max(body.GetPreferredValues(width, 0f).y, body.preferredHeight) + 32f;
            body.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
            scroll.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
            Canvas.ForceUpdateCanvases();
        }

        private bool IsLookingAtList()
        {
            Camera cam = Camera.main;
            if (cam == null || rect == null)
                return false;

            Vector3 toPanel = rect.position - cam.transform.position;
            float distSq = toPanel.sqrMagnitude;
            if (distSq > 25f || distSq < 0.01f)
                return false;

            return Vector3.Dot(cam.transform.forward, toPanel.normalized) > 0.35f;
        }

        private bool LaserHitsThis(out XRNode hand, out Vector3 hitPoint)
        {
            return HitsRect(rect, 48f, 72f, out hand, out hitPoint);
        }

        private bool LaserHitsBar(out XRNode hand, out Vector3 hitPoint)
        {
            hand = XRNode.RightHand;
            hitPoint = Vector3.zero;
            if (bar == null)
                return false;
            return HitsRect(bar.transform as RectTransform, 20f, 20f, out hand, out hitPoint);
        }

        private static bool HitsRect(RectTransform target, float padX, float padY, out XRNode hand, out Vector3 hitPoint)
        {
            hand = XRNode.RightHand;
            hitPoint = Vector3.zero;
            if (target == null)
                return false;

            foreach (var node in new[] { XRNode.RightHand, XRNode.LeftHand })
            {
                if (!VRMenuDragger.TryGetPointerAim(node, out Ray aim, out _))
                    continue;

                var plane = new Plane(target.forward, target.position);
                if (!plane.Raycast(aim, out float enter) || enter <= 0f || enter > 8f)
                {
                    plane = new Plane(-target.forward, target.position);
                    if (!plane.Raycast(aim, out enter) || enter <= 0f || enter > 8f)
                        continue;
                }

                Vector3 point = aim.GetPoint(enter);
                Vector3 local = target.InverseTransformPoint(point);
                Rect padded = target.rect;
                padded.xMin -= padX;
                padded.xMax += padX;
                padded.yMin -= padY;
                padded.yMax += padY;
                if (!padded.Contains(new Vector2(local.x, local.y)))
                    continue;

                hand = node;
                hitPoint = point;
                return true;
            }

            return false;
        }

        private static bool TryAnyStickY(out float stickY)
        {
            if (TryActionY(rightStickAction, out stickY) || TryActionY(leftStickAction, out stickY))
                return true;
            return TryDeviceY(XRNode.RightHand, out stickY) || TryDeviceY(XRNode.LeftHand, out stickY);
        }

        private static bool TryActionY(InputAction action, out float stickY)
        {
            stickY = 0f;
            if (action == null)
                return false;
            Vector2 axis = action.ReadValue<Vector2>();
            stickY = axis.y;
            return Mathf.Abs(stickY) > 0.18f;
        }

        private static bool TryDeviceY(XRNode hand, out float stickY)
        {
            stickY = 0f;
            UnityEngine.XR.InputDevice device = InputDevices.GetDeviceAtXRNode(hand);
            if (!device.isValid)
                return false;
            if (!device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.primary2DAxis, out Vector2 axis))
                return false;
            stickY = axis.y;
            return Mathf.Abs(stickY) > 0.18f;
        }

        private static bool IsTriggerDown(XRNode hand)
        {
            UnityEngine.XR.InputDevice device = InputDevices.GetDeviceAtXRNode(hand);
            if (!device.isValid)
                return false;
            if (device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.triggerButton, out bool pressed) && pressed)
                return true;
            return device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.trigger, out float value) && value >= 0.45f;
        }

        private static void EnsureStickActions()
        {
            if (rightStickAction != null)
                return;

            rightStickAction = new InputAction(type: InputActionType.Value);
            rightStickAction.AddBinding("<XRController>{RightHand}/thumbstick");
            rightStickAction.AddBinding("<XRController>{RightHand}/primary2DAxis");
            rightStickAction.Enable();

            leftStickAction = new InputAction(type: InputActionType.Value);
            leftStickAction.AddBinding("<XRController>{LeftHand}/thumbstick");
            leftStickAction.AddBinding("<XRController>{LeftHand}/primary2DAxis");
            leftStickAction.Enable();
        }
    }
}
