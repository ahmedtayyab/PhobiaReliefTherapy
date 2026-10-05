using UnityEngine;

namespace PhobiaReliefTherapy.VR
{
    /// <summary>
    /// Remembers the last grip-placed menu or graph pose so the next screen opens there.
    /// </summary>
    public static class VrMenuPose
    {
        public static bool HasScreen;
        public static bool HasHud;

        private static Vector3 screenPos;
        private static Quaternion screenRot;
        private static float screenScale = VrWorldMenu.DefaultScale;

        private static Vector3 hudPos;
        private static Quaternion hudRot;
        private static float hudScale = 0.00105f;

        public static void CaptureScreen(Transform t)
        {
            if (t == null)
                return;
            float scale = t.localScale.x;
            if (scale < 0.0003f || scale > 0.0025f)
                return;
            screenPos = t.position;
            screenRot = t.rotation;
            screenScale = Mathf.Clamp(scale, 0.00032f, 0.0016f);
            HasScreen = true;
        }

        public static void CaptureHud(Transform t)
        {
            if (t == null)
                return;
            hudPos = t.position;
            hudRot = t.rotation;
            hudScale = Mathf.Clamp(t.localScale.x, 0.00032f, 0.0016f);
            HasHud = true;
        }

        public static bool TryApplyScreen(Transform t, Camera camera = null)
        {
            if (!HasScreen || t == null)
                return false;

            if (camera != null)
            {
                Vector3 toMenu = screenPos - camera.transform.position;
                float distance = toMenu.magnitude;
                if (distance < 0.35f || distance > 3.6f)
                    return false;
                if (distance > 0.01f && Vector3.Dot(camera.transform.forward, toMenu.normalized) < 0.12f)
                    return false;
            }

            t.position = screenPos;
            t.rotation = screenRot;
            t.localScale = Vector3.one * screenScale;
            return true;
        }

        public static bool TryApplyHud(Transform t, Camera camera = null)
        {
            if (!HasHud || t == null)
                return false;

            if (camera != null)
            {
                Vector3 toHud = hudPos - camera.transform.position;
                float distance = toHud.magnitude;
                if (distance < 0.3f || distance > 3.6f)
                    return false;
                if (distance > 0.01f && Vector3.Dot(camera.transform.forward, toHud.normalized) < 0.05f)
                    return false;
            }

            t.position = hudPos;
            t.rotation = hudRot;
            t.localScale = Vector3.one * hudScale;
            return true;
        }
    }
}
