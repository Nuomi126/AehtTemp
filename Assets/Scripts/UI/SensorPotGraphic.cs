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

        // The widening sides match the chamber outline in the legacy display.
        Vector2[] outline =
        {
            new(0.095f, 0.94f), new(0.905f, 0.94f),
            new(0.95f, 0.78f), new(0.98f, 0.55f), new(0.98f, 0.45f),
            new(0.95f, 0.22f), new(0.905f, 0.06f), new(0.095f, 0.06f),
            new(0.05f, 0.22f), new(0.02f, 0.45f), new(0.02f, 0.55f),
            new(0.05f, 0.78f)
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
