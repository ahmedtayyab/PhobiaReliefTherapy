using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;
using PhobiaReliefTherapy.Managers;
using PhobiaReliefTherapy.UI;

namespace PhobiaReliefTherapy.VR
{
    /// <summary>
    /// Puts a UI canvas in World Space in front of the headset so it is visible in VR
    /// and can be grip-dragged. Overlay canvases only appear on the PC Game view.
    /// </summary>
    public static class VrWorldMenu
    {
        public const float DefaultDistance = 1.45f;
        public const float DefaultScale = 0.00062f;

        public static void AttachAndPlace(Canvas canvas, Camera camera, bool recenterIfLoose = true)
        {
            if (canvas == null || camera == null || IsProtected(canvas))
                return;

            var dragger = canvas.GetComponent<VRMenuDragger>();
            if (dragger != null && dragger.UserPlaced && canvas.renderMode == RenderMode.WorldSpace)
            {
                EnsureInteraction(canvas);
                dragger.RefreshCollider();
                return;
            }

            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = camera;
            if (recenterIfLoose && !VrMenuPose.TryApplyScreen(canvas.transform, camera))
                PlaceInFront(canvas, camera);

            EnsureInteraction(canvas);
            SoftenFullScreenBackdrop(canvas);
            if (dragger == null)
                dragger = canvas.gameObject.AddComponent<VRMenuDragger>();
            dragger.RefreshCollider();
        }

        public static void PlaceInFront(Canvas canvas, Camera camera, float distance = DefaultDistance)
        {
            if (canvas == null)
                return;

            if (VrMenuPose.TryApplyScreen(canvas.transform, camera))
                return;

            if (camera == null)
                return;

            canvas.transform.position = camera.transform.position
                                        + camera.transform.forward * distance
                                        + camera.transform.up * -0.06f;
            canvas.transform.rotation = Quaternion.LookRotation(camera.transform.forward, camera.transform.up);
            canvas.transform.localScale = Vector3.one * DefaultScale;
        }

        public static Camera FindVrCamera()
        {
            if (Camera.main != null)
                return Camera.main;
            return Object.FindObjectOfType<Camera>();
        }

        private static void EnsureInteraction(Canvas canvas)
        {
            if (canvas.GetComponent<GraphicRaycaster>() == null)
                canvas.gameObject.AddComponent<GraphicRaycaster>();
            if (canvas.GetComponent<TrackedDeviceGraphicRaycaster>() == null)
                canvas.gameObject.AddComponent<TrackedDeviceGraphicRaycaster>();
        }

        private static void SoftenFullScreenBackdrop(Canvas canvas)
        {
            foreach (var image in canvas.GetComponentsInChildren<Image>(true))
            {
                if (image == null || image.GetComponent<Button>() != null)
                    continue;
                var rect = image.rectTransform;
                bool full = rect.anchorMin == Vector2.zero && rect.anchorMax == Vector2.one;
                if (!full)
                    continue;
                Color c = image.color;
                if (c.a > 0.35f)
                    image.color = new Color(c.r, c.g, c.b, 0.22f);
                image.raycastTarget = false;
            }
        }

        private static bool IsProtected(Canvas canvas)
        {
            if (canvas.GetComponentInParent<SceneLoader>() != null)
                return true;
            if (canvas.GetComponentInParent<SessionVitalsOverlay>() != null)
                return true;
            return canvas.name == "TransitionCanvas" || canvas.name == "VitalsOverlayCanvas";
        }
    }
}
