namespace RubiksCube;

internal sealed class Cubie(Vector3i pos)
{
    public Vector3i Pos = pos;
    public readonly Dictionary<Direction, StickerColor> Stickers = new();
}