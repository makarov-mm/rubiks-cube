using System.Numerics;

namespace RubiksCube.Puzzle;

internal readonly record struct Vector3i(int X, int Y, int Z)
{
    public Vector3 ToVector3() => new(X, Y, Z);
    public static int Dot(Vector3i a, Vector3i b) => a.X * b.X + a.Y * b.Y + a.Z * b.Z;

    // The face a unit axis vector points to.
    public Face ToFace() => this switch
    {
        (1, 0, 0)  => Face.Right,
        (-1, 0, 0) => Face.Left,
        (0, 1, 0)  => Face.Up,
        (0, -1, 0) => Face.Down,
        (0, 0, 1)  => Face.Front,
        (0, 0, -1) => Face.Back,
        _ => throw new InvalidOperationException($"{this} is not a unit axis vector."),
    };
}
