using System.Numerics;
using System.Runtime.InteropServices;

namespace RubiksCube.Rendering;

[StructLayout(LayoutKind.Sequential)]
internal readonly struct Vertex(Vector3 position, Vector3 normal, Vector2 uv)
{
    public readonly Vector3 Position = position;
    public readonly Vector3 Normal = normal;
    public readonly Vector2 Uv = uv;
}