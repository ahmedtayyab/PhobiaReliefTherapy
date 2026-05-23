using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace PhobiaReliefTherapy.UI
{
    [ExecuteAlways]
    [RequireComponent(typeof(Image))]
    [DisallowMultipleComponent]
    public class PremiumBackgroundController : MonoBehaviour
    {
        [Header("Gradient Colors")]
        public Color topColor = new Color32(18, 42, 80, 255);
        public Color bottomColor = new Color32(8, 16, 32, 255);
        public Color accentColor = new Color32(102, 159, 218, 64);

        [Header("Accent Settings")]
        public Vector2 accentPosition = new Vector2(0.75f, 0.2f);
        public Vector2 accentSize = new Vector2(0.55f, 0.35f);
        public float accentAlpha = 0.18f;

        [Header("Depth Overlay")]
        public Color vignetteColor = new Color32(0, 0, 0, 48);

        private Image backgroundImage;
        private UIGradient gradient;

        private void Awake()
        {
            SetupBackground();
            CreateAccentShape();
            CreateVignetteOverlay();
        }

        private void OnEnable()
        {
            SetupBackground();
            CreateAccentShape();
            CreateVignetteOverlay();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            SetupBackground();
            CreateAccentShape();
            CreateVignetteOverlay();
        }
#endif

        private void SetupBackground()
        {
            backgroundImage = GetComponent<Image>();
            backgroundImage.raycastTarget = false;
            backgroundImage.color = bottomColor;
            backgroundImage.rectTransform.anchorMin = Vector2.zero;
            backgroundImage.rectTransform.anchorMax = Vector2.one;
            backgroundImage.rectTransform.offsetMin = Vector2.zero;
            backgroundImage.rectTransform.offsetMax = Vector2.zero;

            gradient = GetComponent<UIGradient>();
            if (gradient == null)
                gradient = gameObject.AddComponent<UIGradient>();

            gradient.topColor = topColor;
            gradient.bottomColor = bottomColor;
            gradient.vertical = true;
            gradient.flip = false;
        }

        private void CreateAccentShape()
        {
            string childName = "PremiumAccentGlow";
            Transform existing = transform.Find(childName);
            Image accentImage;
            if (existing != null)
            {
                accentImage = existing.GetComponent<Image>();
            }
            else
            {
                GameObject accent = new GameObject(childName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                accent.transform.SetParent(transform, false);
                accentImage = accent.GetComponent<Image>();
                accentImage.raycastTarget = false;
                accentImage.sprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/UISprite.psd");
                accentImage.type = Image.Type.Sliced;
            }

            RectTransform rt = accentImage.rectTransform;
            rt.anchorMin = accentPosition - accentSize * 0.5f;
            rt.anchorMax = accentPosition + accentSize * 0.5f;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            accentImage.color = new Color(accentColor.r, accentColor.g, accentColor.b, accentAlpha);
            accentImage.material = null;
        }

        private void CreateVignetteOverlay()
        {
            string childName = "PremiumVignetteOverlay";
            Transform existing = transform.Find(childName);
            Image overlayImage;
            if (existing != null)
            {
                overlayImage = existing.GetComponent<Image>();
            }
            else
            {
                GameObject overlay = new GameObject(childName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                overlay.transform.SetParent(transform, false);
                overlayImage = overlay.GetComponent<Image>();
                overlayImage.sprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/UISprite.psd");
                overlayImage.type = Image.Type.Sliced;
            }

            RectTransform rt = overlayImage.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            overlayImage.color = vignetteColor;
            overlayImage.raycastTarget = false;
            overlayImage.material = null;
        }
    }
}
