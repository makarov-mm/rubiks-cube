using System.Numerics;

namespace RubiksCube;

internal sealed class Mesh
{
    private readonly uint _vao;
    private readonly uint _vbo;
    private readonly int _vertexCount;

    private Mesh(float[] data)
    {
        _vertexCount = data.Length / 8;

        GL.GenVertexArrays(1, out _vao);
        GL.GenBuffers(1, out _vbo);

        GL.BindVertexArray(_vao);
        GL.BindBuffer(GL.GL_ARRAY_BUFFER, _vbo);
        GL.BufferData(GL.GL_ARRAY_BUFFER, data, GL.GL_STATIC_DRAW);

        int stride = 8 * sizeof(float);
        GL.EnableVertexAttribArray(0);
        GL.VertexAttribPointer(0, 3, GL.GL_FLOAT, false, stride, IntPtr.Zero);

        GL.EnableVertexAttribArray(1);
        GL.VertexAttribPointer(1, 3, GL.GL_FLOAT, false, stride, new IntPtr(3 * sizeof(float)));

        GL.EnableVertexAttribArray(2);
        GL.VertexAttribPointer(2, 2, GL.GL_FLOAT, false, stride, new IntPtr(6 * sizeof(float)));

        GL.BindVertexArray(0);
    }

    public void Draw()
    {
        GL.BindVertexArray(_vao);
        GL.DrawArrays(GL.GL_TRIANGLES, 0, _vertexCount);
        GL.BindVertexArray(0);
    }

    public static Mesh CreateStickerPlane()
    {
        var v = new List<float>();
        AddQuad(v,
            new System.Numerics.Vector3(-0.5f, -0.5f, 0.0f),
            new System.Numerics.Vector3(0.5f, -0.5f, 0.0f),
            new System.Numerics.Vector3(0.5f, 0.5f, 0.0f),
            new System.Numerics.Vector3(-0.5f, 0.5f, 0.0f),
            System.Numerics.Vector3.UnitZ);
        return new Mesh(v.ToArray());
    }

    public static Mesh CreateFloorPlane()
    {
        var v = new List<float>();
        AddVertex(v, new System.Numerics.Vector3(-0.5f, 0.0f, -0.5f), System.Numerics.Vector3.UnitY, new Vector2(0, 0));
        AddVertex(v, new System.Numerics.Vector3(0.5f, 0.0f, -0.5f), System.Numerics.Vector3.UnitY, new Vector2(1, 0));
        AddVertex(v, new System.Numerics.Vector3(0.5f, 0.0f, 0.5f), System.Numerics.Vector3.UnitY, new Vector2(1, 1));
        AddVertex(v, new System.Numerics.Vector3(-0.5f, 0.0f, -0.5f), System.Numerics.Vector3.UnitY, new Vector2(0, 0));
        AddVertex(v, new System.Numerics.Vector3(0.5f, 0.0f, 0.5f), System.Numerics.Vector3.UnitY, new Vector2(1, 1));
        AddVertex(v, new System.Numerics.Vector3(-0.5f, 0.0f, 0.5f), System.Numerics.Vector3.UnitY, new Vector2(0, 1));
        return new Mesh(v.ToArray());
    }

    public static Mesh CreateCube()
    {
        var v = new List<float>();

        AddQuad(v,
            new System.Numerics.Vector3(-0.5f, -0.5f, 0.5f),
            new System.Numerics.Vector3(0.5f, -0.5f, 0.5f),
            new System.Numerics.Vector3(0.5f, 0.5f, 0.5f),
            new System.Numerics.Vector3(-0.5f, 0.5f, 0.5f),
            System.Numerics.Vector3.UnitZ);

        AddQuad(v,
            new System.Numerics.Vector3(0.5f, -0.5f, -0.5f),
            new System.Numerics.Vector3(-0.5f, -0.5f, -0.5f),
            new System.Numerics.Vector3(-0.5f, 0.5f, -0.5f),
            new System.Numerics.Vector3(0.5f, 0.5f, -0.5f),
            -System.Numerics.Vector3.UnitZ);

        AddQuad(v,
            new System.Numerics.Vector3(0.5f, -0.5f, 0.5f),
            new System.Numerics.Vector3(0.5f, -0.5f, -0.5f),
            new System.Numerics.Vector3(0.5f, 0.5f, -0.5f),
            new System.Numerics.Vector3(0.5f, 0.5f, 0.5f),
            System.Numerics.Vector3.UnitX);

        AddQuad(v,
            new System.Numerics.Vector3(-0.5f, -0.5f, -0.5f),
            new System.Numerics.Vector3(-0.5f, -0.5f, 0.5f),
            new System.Numerics.Vector3(-0.5f, 0.5f, 0.5f),
            new System.Numerics.Vector3(-0.5f, 0.5f, -0.5f),
            -System.Numerics.Vector3.UnitX);

        AddQuad(v,
            new System.Numerics.Vector3(-0.5f, 0.5f, 0.5f),
            new System.Numerics.Vector3(0.5f, 0.5f, 0.5f),
            new System.Numerics.Vector3(0.5f, 0.5f, -0.5f),
            new System.Numerics.Vector3(-0.5f, 0.5f, -0.5f),
            System.Numerics.Vector3.UnitY);

        AddQuad(v,
            new System.Numerics.Vector3(-0.5f, -0.5f, -0.5f),
            new System.Numerics.Vector3(0.5f, -0.5f, -0.5f),
            new System.Numerics.Vector3(0.5f, -0.5f, 0.5f),
            new System.Numerics.Vector3(-0.5f, -0.5f, 0.5f),
            -System.Numerics.Vector3.UnitY);

        return new Mesh(v.ToArray());
    }

    private static void AddQuad(List<float> v, System.Numerics.Vector3 a, System.Numerics.Vector3 b, System.Numerics.Vector3 c, System.Numerics.Vector3 d, System.Numerics.Vector3 n)
    {
        AddVertex(v, a, n, new Vector2(0, 0));
        AddVertex(v, b, n, new Vector2(1, 0));
        AddVertex(v, c, n, new Vector2(1, 1));
        AddVertex(v, a, n, new Vector2(0, 0));
        AddVertex(v, c, n, new Vector2(1, 1));
        AddVertex(v, d, n, new Vector2(0, 1));
    }

    private static void AddVertex(List<float> v, System.Numerics.Vector3 p, System.Numerics.Vector3 n, Vector2 uv)
    {
        v.Add(p.X); v.Add(p.Y); v.Add(p.Z);
        v.Add(n.X); v.Add(n.Y); v.Add(n.Z);
        v.Add(uv.X); v.Add(uv.Y);
    }
}