namespace RubiksCube.Puzzle;

// The puzzle: 27 cubies, the moves waiting their turn and the animation of the current one.
internal sealed class Cube
{
    public const float TurnSeconds = 0.18f;
    private const int ScrambleLength = 28;

    private readonly List<Cubie> _cubies = new(27);
    private readonly Queue<Move> _pending = new();
    private float _turnTime;

    public Cube() => Reset();

    public IReadOnlyList<Cubie> Cubies => _cubies;

    // The move being animated; the cubies take their new places when it ends.
    public Move? CurrentMove { get; private set; }

    // How far the current move has turned, 0 to 1, eased in and out.
    public float CurrentProgress
    {
        get
        {
            float t = Math.Clamp(_turnTime / TurnSeconds, 0f, 1f);
            return t * t * (3f - 2f * t);
        }
    }

    // The solved cube; pending moves are dropped.
    public void Reset()
    {
        _pending.Clear();
        CurrentMove = null;
        _turnTime = 0f;

        _cubies.Clear();
        for (int x = -1; x <= 1; x++)
            for (int y = -1; y <= 1; y++)
                for (int z = -1; z <= 1; z++)
                {
                    var position = new Vector3i(x, y, z);
                    // A cubie carries a sticker on each face of the cube it lies on.
                    var stickers = Enum.GetValues<Face>()
                        .Where(face => Vector3i.Dot(position, face.Normal) == 1)
                        .ToDictionary(face => face, face => face.SolvedColor);
                    _cubies.Add(new Cubie(position, stickers));
                }
    }

    // Moves are played one after another, so none is lost while another one is turning.
    public void Turn(Move move) => _pending.Enqueue(move);

    public void Scramble(Random random)
    {
        Move? previous = _pending.Count > 0 ? _pending.Last() : CurrentMove;
        for (int i = 0; i < ScrambleLength; i++)
        {
            Move move;
            do
            {
                move = new Move((Axis)random.Next(3), random.Next(2) == 0 ? -1 : 1, random.Next(2) == 0 ? -1 : 1);
            }
            // Turning the same layer twice in a row would merge the turns or undo the first one.
            while (previous is { } last && last.Axis == move.Axis && last.Layer == move.Layer);

            _pending.Enqueue(move);
            previous = move;
        }
    }

    public void Update(float seconds)
    {
        if (CurrentMove is null && _pending.TryDequeue(out Move next))
        {
            CurrentMove = next;
            _turnTime = 0f;
        }
        if (CurrentMove is not { } move)
            return;

        _turnTime += seconds;
        if (_turnTime < TurnSeconds)
            return;

        foreach (Cubie cubie in _cubies)
            if (move.Contains(cubie.Position))
                cubie.Turn(move);
        CurrentMove = null;
        _turnTime = 0f;
    }
}
