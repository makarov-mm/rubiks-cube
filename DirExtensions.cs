using System.Numerics;

namespace RubiksCube;

internal static class DirExtensions
{
    public static Vector3i ToVec3i(this Direction d)
    {
        return d switch
        {
            Direction.Right => new Vector3i(1, 0, 0),
            Direction.Left => new Vector3i(-1, 0, 0),
            Direction.Up => new Vector3i(0, 1, 0),
            Direction.Down => new Vector3i(0, -1, 0),
            Direction.Front => new Vector3i(0, 0, 1),
            Direction.Back => new Vector3i(0, 0, -1),
            _ => new Vector3i(0, 0, 1)
        };
    }

    public static Vector3 ToVector3(this Direction d) => d.ToVec3i().ToVector3();
}
