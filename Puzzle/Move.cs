using System.Numerics;

namespace RubiksCube.Puzzle;

// A quarter turn of the layer at coordinate Layer (-1, 0 or 1) along Axis. Direction +1
// turns it counterclockwise about the positive axis (the right-hand rule), -1 clockwise.
internal readonly record struct Move(Axis Axis, int Layer, int Direction)
{
    // Standard notation: R, L, U, D, F and B turn their face clockwise as seen looking at
    // that face; the primed moves (R', L', ...) turn it counterclockwise.
    public static Move Of(Face face, bool clockwise)
    {
        Vector3i n = face.Normal;
        (Axis axis, int layer) = n.X != 0 
            ? (Axis.X, n.X) 
            : n.Y != 0 
                ? (Axis.Y, n.Y) 
                : (Axis.Z, n.Z);

        // Seen from outside the face, clockwise is a negative turn about its outward normal.
        return new Move(axis, layer, clockwise ? -layer : layer);
    }

    public bool Contains(Vector3i position) => Axis switch
    {
        Axis.X => position.X == Layer,
        Axis.Y => position.Y == Layer,
        _      => position.Z == Layer,
    };

    public Vector3i Turn(Vector3i p) => (Axis, Direction > 0) switch
    {
        (Axis.X, true)  => new Vector3i(p.X, -p.Z, p.Y),
        (Axis.X, false) => new Vector3i(p.X, p.Z, -p.Y),
        (Axis.Y, true)  => new Vector3i(p.Z, p.Y, -p.X),
        (Axis.Y, false) => new Vector3i(-p.Z, p.Y, p.X),
        (_, true)       => new Vector3i(-p.Y, p.X, p.Z),
        (_, false)      => new Vector3i(p.Y, -p.X, p.Z)
    };

    public Face Turn(Face face) => Turn(face.Normal).ToFace();

    // The rotation of the turning layer, `fraction` of the way through the turn.
    public Matrix4x4 Rotation(float fraction)
    {
        float angle = Direction * fraction * MathF.PI * 0.5f;

        return Axis switch
        {
            Axis.X => Matrix4x4.CreateRotationX(angle),
            Axis.Y => Matrix4x4.CreateRotationY(angle),
            _      => Matrix4x4.CreateRotationZ(angle),
        };
    }
}
