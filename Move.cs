namespace RubiksCube;

internal readonly record struct Move(Axis Axis, int Layer, int Dir);