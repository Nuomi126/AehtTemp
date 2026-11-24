using UnityEngine;
using UnityEngine.UI;

/// <summary>Yellow electrolytic-cell silhouette used behind the live sensor labels.</summary>
public sealed class SensorPotGraphic : MaskableGraphic
{
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        Rect r = GetPixelAdjustedRect();
        if (r.width <= 0 || r.height <= 0) return;

        // Keep the shoulders broad and use several symmetric steps on each
        // side so the edge reads as a smooth chamber wall at phone sizes.
        Vector2[] outline =
        {
            new(0.080f, 0.935f), new(0.920f, 0.935f),
            new(0.938f, 0.875f), new(0.956f, 0.790f), new(0.970f, 0.690f),
            new(0.980f, 0.580f), new(0.983f, 0.500f), new(0.980f, 0.420f),
            new(0.970f, 0.310f), new(0.956f, 0.210f), new(0.938f, 0.125f),
            new(0.920f, 0.065f), new(0.080f, 0.065f),
            new(0.062f, 0.125f), new(0.044f, 0.210f), new(0.030f, 0.310f),
            new(0.020f, 0.420f), new(0.017f, 0.500f), new(0.020f, 0.580f),
            new(0.030f, 0.690f), new(0.044f, 0.790f), new(0.062f, 0.875f)
        };
        AddPolygon(vh, r, outline, new Color32(23, 23, 23, 255));
        var inset = new Vector2[outline.Length];
        for (int i = 0; i < outline.Length; i++)
            inset[i] = Vector2.Lerp(outline[i], new Vector2(0.5f, 0.5f), 0.006f);
        AddPolygon(vh, r, inset, new Color32(255, 184, 0, 255));
    }

    private static void AddPolygon(VertexHelper vh, Rect r, Vector2[] points, Color32 color)
    {
        int first = vh.currentVertCount;
        foreach (Vector2 p in points)
        {
            var v = UIVertex.simpleVert;
            v.color = color;
            v.position = new Vector3(r.xMin + p.x * r.width, r.yMin + p.y * r.height);
            vh.AddVert(v);
        }
        for (int i = 1; i < points.Length - 1; i++)
            vh.AddTriangle(first, first + i, first + i + 1);
    }
}
