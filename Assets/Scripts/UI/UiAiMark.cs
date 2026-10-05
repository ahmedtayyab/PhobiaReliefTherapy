using UnityEngine;
using UnityEngine.UI;

namespace PhobiaReliefTherapy.UI
{
    /// <summary>
    /// Animated AI mark: core, orbiting nodes, and a sweeping ring. Same stroke language as the graphs.
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public class UiAiMark : MaskableGraphic
    {
        public Color Accent = new Color32(255, 87, 34, 255);
        public bool ThinkingFast;

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            Rect rect = GetPixelAdjustedRect();
            Vector2 center = rect.center;
            float radius = Mathf.Min(rect.width, rect.height) * 0.42f;
            float t = Time.unscaledTime;
            float spin = ThinkingFast ? 2.8f : 1.15f;

            Color dim = new Color(Accent.r, Accent.g, Accent.b, 0.22f);
            Color mid = new Color(Accent.r, Accent.g, Accent.b, 0.55f);
            Color hot = new Color(Accent.r, Accent.g, Accent.b, 0.95f);

            DrawRing(vh, center, radius, 2.1f, 0f, 360f, dim);
            DrawRing(vh, center, radius * 0.68f, 1.4f, 0f, 360f, mid);

            float sweep = 118f;
            float start = (t * 140f * spin) % 360f;
            DrawRing(vh, center, radius, 3.2f, start, sweep, hot);

            float core = radius * (0.18f + 0.05f * Mathf.Sin(t * (ThinkingFast ? 10f : 4f)));
            DrawDisc(vh, center, core, hot);

            for (int i = 0; i < 3; i++)
            {
                float angle = (t * 90f * spin + i * 120f) * Mathf.Deg2Rad;
                Vector2 p = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * (radius * 0.68f);
                float pulse = 0.7f + 0.3f * Mathf.Sin(t * 6f + i);
                DrawDisc(vh, p, 2.6f * pulse, hot);
            }
        }

        public void Tick()
        {
            SetVerticesDirty();
        }

        private static void DrawDisc(VertexHelper vh, Vector2 center, float radius, Color color)
        {
            const int steps = 16;
            int start = vh.currentVertCount;
            vh.AddVert(center, color, Vector2.zero);
            for (int i = 0; i <= steps; i++)
            {
                float a = i / (float)steps * Mathf.PI * 2f;
                vh.AddVert(center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius, color, Vector2.zero);
            }

            for (int i = 1; i <= steps; i++)
                vh.AddTriangle(start, start + i, start + i + 1);
        }

        private static void DrawRing(VertexHelper vh, Vector2 center, float radius, float thickness, float startDeg, float sweepDeg, Color color)
        {
            const int steps = 28;
            float start = startDeg * Mathf.Deg2Rad;
            float sweep = sweepDeg * Mathf.Deg2Rad;
            float inner = radius - thickness * 0.5f;
            float outer = radius + thickness * 0.5f;

            for (int i = 0; i < steps; i++)
            {
                float a0 = start + sweep * (i / (float)steps);
                float a1 = start + sweep * ((i + 1) / (float)steps);
                Vector2 i0 = center + new Vector2(Mathf.Cos(a0), Mathf.Sin(a0)) * inner;
                Vector2 o0 = center + new Vector2(Mathf.Cos(a0), Mathf.Sin(a0)) * outer;
                Vector2 i1 = center + new Vector2(Mathf.Cos(a1), Mathf.Sin(a1)) * inner;
                Vector2 o1 = center + new Vector2(Mathf.Cos(a1), Mathf.Sin(a1)) * outer;
                int v = vh.currentVertCount;
                vh.AddVert(i0, color, Vector2.zero);
                vh.AddVert(o0, color, Vector2.zero);
                vh.AddVert(o1, color, Vector2.zero);
                vh.AddVert(i1, color, Vector2.zero);
                vh.AddTriangle(v, v + 1, v + 2);
                vh.AddTriangle(v, v + 2, v + 3);
            }
        }
    }
}
