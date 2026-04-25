namespace RubiksCube;

internal readonly record struct Vector3(int X, int Y, int Z)
{
    public System.Numerics.Vector3 ToVector3() => new(X, Y, Z);

    public Direction ToDir()
    {
        if (X == 1) return Direction.Right;
        if (X == -1) return Direction.Left;
        if (Y == 1) return Direction.Up;
        if (Y == -1) return Direction.Down;
        if (Z == 1) return Direction.Front;
        return Direction.Back;
    }
}