namespace RubiksCube.Puzzle;

// One of the 27 small cubes: where it is, and which colour faces which way.
internal sealed class Cubie(Vector3i position, IReadOnlyDictionary<Face, StickerColor> stickers)
{
    public Vector3i Position { get; private set; } = position;
    public IReadOnlyDictionary<Face, StickerColor> Stickers { get; private set; } = stickers;

    public void Turn(Move move)
    {
        Position = move.Turn(Position);
        Stickers = Stickers.ToDictionary(sticker => move.Turn(sticker.Key), sticker => sticker.Value);
    }
}
