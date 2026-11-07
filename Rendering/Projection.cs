using System.Numerics;

namespace RubiksCube.Rendering;

// Projections for OpenGL's clip space, where depth runs from -1 to 1. Matrix4x4's own
// CreatePerspective* and CreateOrthographic* are made for Direct3D's 0 to 1.
internal static class Projection
{
    public static Matrix4x4 Perspective(float fieldOfViewY, float aspect, float near, float far)
    {
        float f = 1.0f / MathF.Tan(fieldOfViewY * 0.5f);

        return new Matrix4x4(
            f / aspect, 0, 0, 0,
            0, f, 0, 0,
            0, 0, (far + near) / (near - far), -1,
            0, 0, (2.0f * far * near) / (near - far), 0);
    }

    public static Matrix4x4 Orthographic(float left, float right, float bottom, float top, float near, float far) => new(
        2.0f / (right - left), 0, 0, 0,
        0, 2.0f / (top - bottom), 0, 0,
        0, 0, -2.0f / (far - near), 0,
        -(right + left) / (right - left), -(top + bottom) / (top - bottom), -(far + near) / (far - near), 1);
}
