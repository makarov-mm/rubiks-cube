using System.Numerics;
using RubiksCube.Puzzle;

namespace RubiksCube.Rendering;

// Draws the scene: the cube, its reflection in the floor, the glowing floor grid and
// the help overlay. Needs the OpenGL context to be current for its whole lifetime.
internal sealed class CubeRenderer : IDisposable
{
    private const float CubieSpacing = 1.08f;
    private const float BodySize = 0.94f;
    private const float StickerSize = 0.74f;
    private const float StickerOffset = BodySize * 0.5f + 0.015f;   // just above the body
    private const float FloorY = -2.10f;
    private const float FieldOfView = 0.72f;                        // vertical, radians

    private static readonly Vector3 BodyColor = new(0.006f, 0.008f, 0.012f);
    private static readonly Matrix4x4 Mirror = Matrix4x4.CreateScale(1f, -1f, 1f) * Matrix4x4.CreateTranslation(0f, 2f * FloorY, 0f);

    private static readonly string[] HelpLines =
    [
        "CONTROLS",
        "",
        "MOUSE DRAG   ROTATE VIEW",
        "MOUSE WHEEL  ZOOM",
        "",
        "U D L R F B  ROTATE FACES",
        "SHIFT + KEY  REVERSE MOVE",
        "S            SCRAMBLE",
        "CTRL + R     RESET CUBE",
        "HOME         RESET CAMERA",
        "ESC          CLOSE",
    ];

    private enum Material { Sticker, Body, Floor, Overlay } // uMaterial in cube.frag

    private readonly ShaderProgram _program;
    private readonly Mesh _cubieMesh;
    private readonly Mesh _stickerMesh;
    private readonly Mesh _floorMesh;
    private readonly Mesh _panelMesh;
    private readonly Mesh _textMesh;

    private readonly int _uModel;
    private readonly int _uView;
    private readonly int _uProjection;
    private readonly int _uColor;
    private readonly int _uCameraPos;
    private readonly int _uTime;
    private readonly int _uMaterial;
    private readonly int _uAlpha;
    private readonly int _uReflection;

    public CubeRenderer()
    {
        _program = new ShaderProgram(ReadShader("cube.vert"), ReadShader("cube.frag"));
        _uModel = _program.Uniform("uModel");
        _uView = _program.Uniform("uView");
        _uProjection = _program.Uniform("uProjection");
        _uColor = _program.Uniform("uColor");
        _uCameraPos = _program.Uniform("uCameraPos");
        _uTime = _program.Uniform("uTime");
        _uMaterial = _program.Uniform("uMaterial");
        _uAlpha = _program.Uniform("uAlpha");
        _uReflection = _program.Uniform("uReflection");

        _cubieMesh = new Mesh(Shapes.Cube());
        _stickerMesh = new Mesh(Shapes.Sticker());
        _floorMesh = new Mesh(Shapes.Floor());
        _panelMesh = new Mesh(Shapes.Rectangle(14f, 14f, 330f, 224f));
        _textMesh = new Mesh(BitmapFont.Layout(HelpLines, 26f, 28f, 2f, 4f));

        Gl.ClearColor(0.003f, 0.005f, 0.010f, 1.0f);
        Gl.DepthFunc(Gl.GL_LEQUAL);
        Gl.CullFace(Gl.GL_BACK);
        Gl.BlendFunc(Gl.GL_SRC_ALPHA, Gl.GL_ONE_MINUS_SRC_ALPHA);
        Gl.Enable(Gl.GL_BLEND);
    }

    // uiScale sizes the overlay, which is laid out in 96-dpi pixels.
    public void Render(Cube cube, Camera camera, int width, int height, float time, float uiScale)
    {
        Gl.Viewport(0, 0, width, height);
        Gl.Clear(Gl.GL_COLOR_BUFFER_BIT | Gl.GL_DEPTH_BUFFER_BIT);
        _program.Use();

        Matrix4x4 scene = Matrix4x4.CreateRotationY(camera.Yaw) * Matrix4x4.CreateRotationX(camera.Pitch);
        Gl.UniformMatrix4(_uView, Matrix4x4.CreateTranslation(0f, 0f, -camera.Distance));
        Gl.UniformMatrix4(_uProjection, Projection.Perspective(FieldOfView, (float)width / height, 0.05f, 80.0f));
        Gl.Uniform3(_uCameraPos, new Vector3(0f, 0f, camera.Distance));
        Gl.Uniform1(_uTime, time);
        Gl.Enable(Gl.GL_DEPTH_TEST);

        // The reflection (the cube mirrored in the floor, dimmed) and the floor are
        // translucent: they write no depth and show both sides of every face. A mirror
        // shows nothing to a camera below it.
        Gl.Disable(Gl.GL_CULL_FACE);
        Gl.DepthMask(false);

        if (camera.Height > FloorY)
        {
            DrawCube(cube, Mirror * scene, reflection: true);
        }

        DrawFloor(scene);
        Gl.DepthMask(true);

        Gl.Enable(Gl.GL_CULL_FACE);
        DrawCube(cube, scene, reflection: false);

        DrawOverlay(width, height, uiScale);
    }

    public void Dispose()
    {
        _textMesh.Dispose();
        _panelMesh.Dispose();
        _floorMesh.Dispose();
        _stickerMesh.Dispose();
        _cubieMesh.Dispose();
        _program.Dispose();
    }

    private void DrawCube(Cube cube, Matrix4x4 world, bool reflection)
    {
        Gl.Uniform1(_uAlpha, reflection ? 0.24f : 1.0f);
        Gl.Uniform1(_uReflection, reflection ? 1 : 0);

        Move? move = cube.CurrentMove;
        Matrix4x4 turn = move?.Rotation(cube.CurrentProgress) ?? Matrix4x4.Identity;

        foreach (Cubie cubie in cube.Cubies)
        {
            bool turning = move is { } m && m.Contains(cubie.Position);
            Matrix4x4 placement = Matrix4x4.CreateTranslation(cubie.Position.ToVector3() * CubieSpacing) * (turning ? turn : Matrix4x4.Identity) * world;
            Draw(_cubieMesh, Matrix4x4.CreateScale(BodySize) * placement, BodyColor, Material.Body);

            foreach ((Face face, StickerColor color) in cubie.Stickers)
            {
                Matrix4x4 model = 
                    Matrix4x4.CreateScale(StickerSize) *
                    Orientation(face) * 
                    Matrix4x4.CreateTranslation(face.Normal.ToVector3() * StickerOffset) *
                    placement;

                Draw(_stickerMesh, model, ColorOf(color), Material.Sticker);
            }
        }
    }

    private void DrawFloor(Matrix4x4 scene)
    {
        Gl.Uniform1(_uReflection, 0);
        Gl.Uniform1(_uAlpha, 0.82f);
        Matrix4x4 model = Matrix4x4.CreateScale(18f, 1f, 18f) * Matrix4x4.CreateTranslation(0f, FloorY, 0f) * scene;
        Draw(_floorMesh, model, new Vector3(0.05f, 0.35f, 0.85f), Material.Floor);
    }

    private void DrawOverlay(int width, int height, float uiScale)
    {
        Gl.Disable(Gl.GL_DEPTH_TEST);
        Gl.Disable(Gl.GL_CULL_FACE);
        Gl.UniformMatrix4(_uView, Matrix4x4.Identity);
        Gl.UniformMatrix4(_uProjection, Projection.Orthographic(0f, width, height, 0f, -1f, 1f));
        Gl.Uniform1(_uReflection, 0);
        Matrix4x4 model = Matrix4x4.CreateScale(uiScale);

        Gl.Uniform1(_uAlpha, 0.58f);
        Draw(_panelMesh, model, new Vector3(0.006f, 0.011f, 0.020f), Material.Overlay);
        Gl.Uniform1(_uAlpha, 0.94f);
        Draw(_textMesh, model, new Vector3(0.70f, 0.88f, 1.00f), Material.Overlay);
    }

    private void Draw(Mesh mesh, in Matrix4x4 model, Vector3 color, Material material)
    {
        Gl.Uniform1(_uMaterial, (int)material);
        Gl.Uniform3(_uColor, color);
        Gl.UniformMatrix4(_uModel, model);
        mesh.Draw();
    }

    // Turns the sticker square, which faces +Z, to face the given way.
    private static Matrix4x4 Orientation(Face face) => face switch
    {
        Face.Back  => Matrix4x4.CreateRotationY(MathF.PI),
        Face.Right => Matrix4x4.CreateRotationY(MathF.PI * 0.5f),
        Face.Left  => Matrix4x4.CreateRotationY(-MathF.PI * 0.5f),
        Face.Up    => Matrix4x4.CreateRotationX(-MathF.PI * 0.5f),
        Face.Down  => Matrix4x4.CreateRotationX(MathF.PI * 0.5f),
        _          => Matrix4x4.Identity,
    };

    private static Vector3 ColorOf(StickerColor color) => color switch
    {
        StickerColor.White  => new Vector3(0.96f, 0.96f, 0.88f),
        StickerColor.Yellow => new Vector3(1.00f, 0.82f, 0.04f),
        StickerColor.Red    => new Vector3(0.95f, 0.04f, 0.05f),
        StickerColor.Orange => new Vector3(1.00f, 0.38f, 0.03f),
        StickerColor.Green  => new Vector3(0.02f, 0.74f, 0.28f),
        StickerColor.Blue   => new Vector3(0.02f, 0.30f, 1.00f),
        _ => Vector3.One,
    };

    // The shaders are embedded resources (see the project file).
    private static string ReadShader(string name)
    {
        using Stream stream = typeof(CubeRenderer).Assembly.GetManifestResourceStream(name) ?? throw new InvalidOperationException($"The shader {name} is missing from the resources.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
