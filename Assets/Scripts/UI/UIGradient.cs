using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Sprites;

namespace PhobiaReliefTherapy.UI
{
    [RequireComponent(typeof(Graphic))]
    [AddComponentMenu("UI/UI Gradient")]
    public class UIGradient : BaseMeshEffect
    {
        public Color topColor = new Color32(45, 90, 145, 255);
        public Color bottomColor = new Color32(12, 24, 56, 255);
        public bool vertical = true;
        public bool flip = false;

        public override void ModifyMesh(VertexHelper vh)
        {
            if (!IsActive() || vh.currentVertCount == 0)
                return;

            UIVertex vertex = default;
            int count = vh.currentVertCount;
            float min = float.MaxValue;
            float max = float.MinValue;

            for (int i = 0; i < count; i++)
            {
                vh.PopulateUIVertex(ref vertex, i);
                float value = vertical ? vertex.position.y : vertex.position.x;
                min = Mathf.Min(min, value);
                max = Mathf.Max(max, value);
            }

            float range = max - min;
            if (Mathf.Approximately(range, 0f))
                range = 1f;

            for (int i = 0; i < count; i++)
            {
                vh.PopulateUIVertex(ref vertex, i);
                float value = vertical ? vertex.position.y : vertex.position.x;
                float normalized = Mathf.InverseLerp(min, max, value);
                if (flip)
                    normalized = 1f - normalized;

                vertex.color = Color32.Lerp(bottomColor, topColor, normalized);
                vh.SetUIVertex(vertex, i);
            }
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            if (graphic != null)
                graphic.SetVerticesDirty();
        }
#endif
    }
}
