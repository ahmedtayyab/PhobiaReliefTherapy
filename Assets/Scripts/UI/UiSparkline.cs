using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace PhobiaReliefTherapy.UI
{
    /// <summary>
    /// Lightweight filled sparkline for live vitals cards.
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public class UiSparkline : MaskableGraphic
    {
        public readonly List<float> Values = new List<float>();
        public readonly List<float> SecondaryValues = new List<float>();

        public float MinValue = 0f;
        public float MaxValue = 100f;
        public bool AutoScale;
        public float LineThickness = 2.4f;
        public Color LineColor = new Color(1f, 0.45f, 0.22f, 1f);
        public Color FillColor = new Color(1f, 0.45f, 0.22f, 0.22f);
        public Color SecondaryColor = new Color(1f, 0.82f, 0.45f, 0.9f);
        public float[] Thresholds;

        public void SetSeries(List<float> source, List<float> secondary = null)
        {
            Values.Clear();
            SecondaryValues.Clear();
            if (source != null)
                Values.AddRange(source);
            if (secondary != null)
                SecondaryValues.AddRange(secondary);
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            Rect rect = GetPixelAdjustedRect();
            DrawThresholds(vh, rect);
            DrawSeries(vh, rect, Values, LineColor, FillColor, true);
            if (SecondaryValues.Count >= 2)
                DrawSeries(vh, rect, SecondaryValues, SecondaryColor, Color.clear, false);
        }

        private void DrawThresholds(VertexHelper vh, Rect rect)
        {
            if (Thresholds == null)
                return;

            Color band = new Color(1f, 1f, 1f, 0.08f);
            for (int i = 0; i < Thresholds.Length; i++)
            {
                float y = YFor(Thresholds[i], rect);
                AddLine(vh, new Vector2(rect.xMin, y), new Vector2(rect.xMax, y), 1.2f, band);
            }
        }

        private void DrawSeries(VertexHelper vh, Rect rect, List<float> series, Color line, Color fill, bool filled)
        {
            if (series == null || series.Count < 2)
                return;

            GetRange(series, out float min, out float max);
            float span = Mathf.Max(1f, max - min);
            int count = series.Count;
            var points = new Vector2[count];
            for (int i = 0; i < count; i++)
            {
                float x = Mathf.Lerp(rect.xMin, rect.xMax, i / (float)(count - 1));
                float y = Mathf.Lerp(rect.yMin, rect.yMax, (series[i] - min) / span);
                points[i] = new Vector2(x, y);
            }

            if (filled && fill.a > 0.01f)
            {
                for (int i = 0; i < count - 1; i++)
                {
                    AddQuad(vh,
                        new Vector2(points[i].x, rect.yMin),
                        points[i],
                        points[i + 1],
                        new Vector2(points[i + 1].x, rect.yMin),
                        fill);
                }
            }

            for (int i = 0; i < count - 1; i++)
                AddLine(vh, points[i], points[i + 1], LineThickness, line);
        }

        private void GetRange(List<float> series, out float min, out float max)
        {
            min = MinValue;
            max = MaxValue;
            if (!AutoScale)
                return;

            min = series[0];
            max = series[0];
            for (int i = 1; i < series.Count; i++)
            {
                if (series[i] < min) min = series[i];
                if (series[i] > max) max = series[i];
            }

            if (max - min < 4f)
            {
                float mid = (min + max) * 0.5f;
                min = mid - 2f;
                max = mid + 2f;
            }
        }

        private float YFor(float value, Rect rect)
        {
            float min = MinValue;
            float max = Mathf.Max(MinValue + 1f, MaxValue);
            return Mathf.Lerp(rect.yMin, rect.yMax, Mathf.InverseLerp(min, max, value));
        }

        private static void AddLine(VertexHelper vh, Vector2 a, Vector2 b, float thickness, Color color)
        {
            Vector2 delta = b - a;
            if (delta.sqrMagnitude < 0.0001f)
                return;

            Vector2 n = new Vector2(-delta.y, delta.x).normalized * (thickness * 0.5f);
            AddQuad(vh, a - n, a + n, b + n, b - n, color);
        }

        private static void AddQuad(VertexHelper vh, Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, Color color)
        {
            int start = vh.currentVertCount;
            vh.AddVert(p0, color, Vector2.zero);
            vh.AddVert(p1, color, Vector2.zero);
            vh.AddVert(p2, color, Vector2.zero);
            vh.AddVert(p3, color, Vector2.zero);
            vh.AddTriangle(start, start + 1, start + 2);
            vh.AddTriangle(start, start + 2, start + 3);
        }
    }
}
