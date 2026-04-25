namespace RubiksCube;

internal sealed class Cubie(Vector3 pos)
{
    public Vector3 Pos = pos;
    public readonly Dictionary<Direction, StickerColor> Stickers = new();
}