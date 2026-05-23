using UnityEngine;

namespace PhobiaReliefTherapy
{
    public static class AutoBindHelper
    {
        public static T FindComponentByName<T>(string objectName) where T : Component
        {
            if (string.IsNullOrEmpty(objectName))
                return null;

            GameObject found = GameObject.Find(objectName);
            return found != null ? found.GetComponent<T>() : null;
        }

        public static T FindComponentInChildrenByName<T>(Transform parent, string childName) where T : Component
        {
            if (parent == null || string.IsNullOrEmpty(childName))
                return null;

            Transform child = FindChildByName(parent, childName);
            return child != null ? child.GetComponent<T>() : null;
        }

        private static Transform FindChildByName(Transform parent, string childName)
        {
            foreach (Transform child in parent)
            {
                if (child.name == childName)
                    return child;

                Transform nested = FindChildByName(child, childName);
                if (nested != null)
                    return nested;
            }

            return null;
        }
    }
}
