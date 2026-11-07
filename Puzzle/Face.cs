namespace RubiksCube.Puzzle;

// The six faces of the cube, named by the direction they face: +X is Right, +Y Up, +Z Front.
internal enum Face { Right, Left, Up, Down, Front, Back }

internal static class FaceExtensions
{
    extension(Face face)
    {
        // The outward unit normal.
        public Vector3i Normal => face switch
        {
            Face.Right => new Vector3i(1, 0, 0),
            Face.Left  => new Vector3i(-1, 0, 0),
            Face.Up    => new Vector3i(0, 1, 0),
            Face.Down  => new Vector3i(0, -1, 0),
            Face.Front => new Vector3i(0, 0, 1),
            Face.Back  => new Vector3i(0, 0, -1),
            _ => throw new ArgumentOutOfRangeException(nameof(face)),
        };

        // The colour of this face on the solved cube.
        public StickerColor SolvedColor => face switch
        {
            Face.Right => StickerColor.Red,
            Face.Left  => StickerColor.Orange,
            Face.Up    => StickerColor.White,
            Face.Down  => StickerColor.Yellow,
            Face.Front => StickerColor.Green,
            Face.Back  => StickerColor.Blue,
            _ => throw new ArgumentOutOfRangeException(nameof(face)),
        };
    }
}
