using System.Numerics;

namespace RubiksCube.Rendering;

// The geometry of the scene. Triangles are wound counterclockwise seen from the front.
internal static class Shapes
{
    // A unit cube around the origin.
    public static Vertex[] Cube()
    {
        var v = new List<Vertex>(36);
        AddQuad(v, new(-0.5f, -0.5f, 0.5f), new(0.5f, -0.5f, 0.5f), new(0.5f, 0.5f, 0.5f), new(-0.5f, 0.5f, 0.5f), Vector3.UnitZ);
        AddQuad(v, new(0.5f, -0.5f, -0.5f), new(-0.5f, -0.5f, -0.5f), new(-0.5f, 0.5f, -0.5f), new(0.5f, 0.5f, -0.5f), -Vector3.UnitZ);
        AddQuad(v, new(0.5f, -0.5f, 0.5f), new(0.5f, -0.5f, -0.5f), new(0.5f, 0.5f, -0.5f), new(0.5f, 0.5f, 0.5f), Vector3.UnitX);
        AddQuad(v, new(-0.5f, -0.5f, -0.5f), new(-0.5f, -0.5f, 0.5f), new(-0.5f, 0.5f, 0.5f), new(-0.5f, 0.5f, -0.5f), -Vector3.UnitX);
        AddQuad(v, new(-0.5f, 0.5f, 0.5f), new(0.5f, 0.5f, 0.5f), new(0.5f, 0.5f, -0.5f), new(-0.5f, 0.5f, -0.5f), Vector3.UnitY);
        AddQuad(v, new(-0.5f, -0.5f, -0.5f), new(0.5f, -0.5f, -0.5f), new(0.5f, -0.5f, 0.5f), new(-0.5f, -0.5f, 0.5f), -Vector3.UnitY);
        return [.. v];
    }

    // A unit square in the XY plane, facing +Z.
    public static Vertex[] Sticker()
    {
        var v = new List<Vertex>(6);
        AddQuad(v, new(-0.5f, -0.5f, 0f), new(0.5f, -0.5f, 0f), new(0.5f, 0.5f, 0f), new(-0.5f, 0.5f, 0f), Vector3.UnitZ);
        return [.. v];
    }

    // A unit square in the XZ plane, uv = (x, z) + 0.5. It is drawn from both sides, so
    // its winding doesn't matter.
    public static Vertex[] Floor()
    {
        var v = new List<Vertex>(6);
        AddQuad(v, new(-0.5f, 0f, -0.5f), new(0.5f, 0f, -0.5f), new(0.5f, 0f, 0.5f), new(-0.5f, 0f, 0.5f), Vector3.UnitY);
        return [.. v];
    }

    // A screen-space rectangle (pixels, y down).
    public static Vertex[] Rectangle(float x, float y, float width, float height)
    {
        var v = new List<Vertex>(6);
        AddRectangle(v, x, y, width, height);
        return [.. v];
    }

    public static void AddRectangle(List<Vertex> v, float x, float y, float width, float height) =>
        AddQuad(v, new(x, y, 0f), new(x + width, y, 0f), new(x + width, y + height, 0f), new(x, y + height, 0f), Vector3.UnitZ);

    // Two triangles, a-b-c and a-c-d, with uv running from (0, 0) at a to (1, 1) at c.
    private static void AddQuad(List<Vertex> v, Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 normal)
    {
        v.Add(new(a, normal, new(0, 0)));
        v.Add(new(b, normal, new(1, 0)));
        v.Add(new(c, normal, new(1, 1)));
        v.Add(new(a, normal, new(0, 0)));
        v.Add(new(c, normal, new(1, 1)));
        v.Add(new(d, normal, new(0, 1)));
    }
}
