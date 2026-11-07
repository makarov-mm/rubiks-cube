namespace RubiksCube.Rendering;

// Looks at the cube from Distance away; the scene turns by Yaw about the vertical axis
// and then by Pitch about the horizontal one.
internal readonly record struct Camera(float Yaw, float Pitch, float Distance)
{
    private const float MaxPitch = 1.25f;
    private const float MinDistance = 4.4f;
    private const float MaxDistance = 18.0f;

    public static Camera Default => new(-0.58f, 0.40f, 8.4f);
    public Camera Orbit(float yaw, float pitch) => this with { Yaw = Yaw + yaw, Pitch = Math.Clamp(Pitch + pitch, -MaxPitch, MaxPitch) };
    public Camera Zoom(float factor) => this with { Distance = Math.Clamp(Distance * factor, MinDistance, MaxDistance) };

    // The camera's height above the centre of the cube.
    public float Height => Distance * MathF.Sin(Pitch);
}
