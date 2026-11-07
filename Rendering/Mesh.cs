using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace RubiksCube.Rendering;

[StructLayout(LayoutKind.Sequential)]
internal readonly struct Vertex(Vector3 position, Vector3 normal, Vector2 uv)
{
    public readonly Vector3 Position = position;
    public readonly Vector3 Normal = normal;
    public readonly Vector2 Uv = uv;
}

// A triangle list in a vertex buffer: position, normal and uv at attribute locations 0, 1 and 2.
internal sealed class Mesh : IDisposable
{
    private readonly uint _vertexArray;
    private readonly uint _vertexBuffer;
    private readonly int _vertexCount;

    public Mesh(ReadOnlySpan<Vertex> vertices)
    {
        _vertexCount = vertices.Length;
        _vertexArray = Gl.GenVertexArray();
        _vertexBuffer = Gl.GenBuffer();

        Gl.BindVertexArray(_vertexArray);
        Gl.BindBuffer(Gl.GL_ARRAY_BUFFER, _vertexBuffer);
        Gl.BufferData(Gl.GL_ARRAY_BUFFER, vertices, Gl.GL_STATIC_DRAW);

        int stride = Unsafe.SizeOf<Vertex>();
        Attribute(0, 3, stride, nameof(Vertex.Position));
        Attribute(1, 3, stride, nameof(Vertex.Normal));
        Attribute(2, 2, stride, nameof(Vertex.Uv));
        Gl.BindVertexArray(0);
    }

    public void Draw()
    {
        Gl.BindVertexArray(_vertexArray);
        Gl.DrawArrays(Gl.GL_TRIANGLES, 0, _vertexCount);
        Gl.BindVertexArray(0);
    }

    public void Dispose()
    {
        Gl.DeleteBuffer(_vertexBuffer);
        Gl.DeleteVertexArray(_vertexArray);
    }

    private static void Attribute(uint location, int size, int stride, string field)
    {
        Gl.EnableVertexAttribArray(location);
        Gl.VertexAttribPointer(location, size, stride, Marshal.OffsetOf<Vertex>(field));
    }
}
