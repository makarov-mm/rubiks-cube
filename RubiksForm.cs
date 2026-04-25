using System.Diagnostics;
using System.Runtime.InteropServices;
using Timer = System.Windows.Forms.Timer;

namespace RubiksCube;

internal sealed class RubiksForm : Form
{
    private IntPtr _hdc;
    private IntPtr _hrc;

    private uint _program;
    private int _uModel;
    private int _uView;
    private int _uProjection;
    private int _uColor;
    private int _uCameraPos;
    private int _uTime;
    private int _uMaterial;
    private int _uAlpha;
    private int _uReflection;

    private Mesh _cubeMesh = null!;
    private Mesh _stickerMesh = null!;
    private Mesh _floorMesh = null!;

    private readonly List<Cubie> _cubies = new();
    private readonly Queue<Move> _moveQueue = new();
    private readonly Random _random = new();
    private Move? _activeMove;
    private float _moveTime;

    private readonly Stopwatch _clock = Stopwatch.StartNew();
    private double _lastSeconds;

    private float _yaw = -0.58f;
    private float _pitch = 0.40f;
    private float _distance = 8.4f;
    private bool _mouseDown;
    private Point _lastMouse;

    private const float CubieSpacing = 1.08f;
    private const float BodySize = 0.94f;
    private const float StickerSize = 0.74f;
    private const float StickerOffset = BodySize * 0.5f + 0.015f;
    private const float FloorY = -2.10f;
    private const float MoveDuration = 0.18f;

    private readonly Timer _timer = new() { Interval = 16 };

    public RubiksForm()
    {
        Text = "Rubik's Cube - C# WinForms OpenGL 4 Shader Demo";
        ClientSize = new Size(1100, 850);
        MinimumSize = new Size(640, 480);
        KeyPreview = true;
        DoubleBuffered = false;
        SetStyle(ControlStyles.Opaque | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint, true);

        _timer.Tick += (_, _) =>
        {
            UpdateAnimation();
            Invalidate(false);
        };
    }

    protected override CreateParams CreateParams
    {
        get
        {
            const int CS_OWNDC = 0x0020;
            var cp = base.CreateParams;
            cp.ClassStyle |= CS_OWNDC;
            return cp;
        }
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        InitOpenGL();
        InitRubiksCube();
        InitScene();
        _lastSeconds = _clock.Elapsed.TotalSeconds;
        _timer.Start();
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _timer.Stop();

        if (_hrc != IntPtr.Zero)
        {
            Wgl.wglMakeCurrent(IntPtr.Zero, IntPtr.Zero);
            Wgl.wglDeleteContext(_hrc);
            _hrc = IntPtr.Zero;
        }

        if (_hdc != IntPtr.Zero)
        {
            Wgl.ReleaseDC(Handle, _hdc);
            _hdc = IntPtr.Zero;
        }

        base.OnFormClosed(e);
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        // OpenGL owns the whole client area.
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        if (_program == 0 || ClientSize.Width <= 0 || ClientSize.Height <= 0)
            return;

        Render();
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        if (_hdc != IntPtr.Zero && ClientSize.Width > 0 && ClientSize.Height > 0)
            GL.Viewport(0, 0, ClientSize.Width, ClientSize.Height);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            _mouseDown = true;
            _lastMouse = e.Location;
            Capture = true;
        }

        base.OnMouseDown(e);
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            _mouseDown = false;
            Capture = false;
        }

        base.OnMouseUp(e);
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        if (_mouseDown)
        {
            int dx = e.X - _lastMouse.X;
            int dy = e.Y - _lastMouse.Y;
            _lastMouse = e.Location;

            _yaw += dx * 0.008f;
            _pitch += dy * 0.008f;
            _pitch = Math.Clamp(_pitch, -1.25f, 1.25f);
        }

        base.OnMouseMove(e);
    }

    protected override void OnMouseWheel(MouseEventArgs e)
    {
        _distance *= e.Delta > 0 ? 0.90f : 1.10f;
        _distance = Math.Clamp(_distance, 4.4f, 18.0f);
        base.OnMouseWheel(e);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        int dir = e.Shift ? -1 : 1;

        switch (e.KeyCode)
        {
            case Keys.U: BeginMove(Axis.Y, 1, dir); break;
            case Keys.D: BeginMove(Axis.Y, -1, -dir); break;
            case Keys.L: BeginMove(Axis.X, -1, -dir); break;
            case Keys.R when e.Control:
                ResetCube();
                break;
            case Keys.R:
                BeginMove(Axis.X, 1, dir);
                break;
            case Keys.F: BeginMove(Axis.Z, 1, dir); break;
            case Keys.B: BeginMove(Axis.Z, -1, -dir); break;
            case Keys.S: Scramble(); break;
            case Keys.Home:
                _yaw = -0.58f;
                _pitch = 0.40f;
                _distance = 8.4f;
                break;
            case Keys.Escape:
                Close();
                break;
        }

        base.OnKeyDown(e);
    }

    private void InitOpenGL()
    {
        _hdc = Wgl.GetDC(Handle);
        if (_hdc == IntPtr.Zero)
            throw new InvalidOperationException("GetDC failed.");

        var pfd = new Wgl.PIXELFORMATDESCRIPTOR
        {
            nSize = (ushort)Marshal.SizeOf<Wgl.PIXELFORMATDESCRIPTOR>(),
            nVersion = 1,
            dwFlags = Wgl.PFD_DRAW_TO_WINDOW | Wgl.PFD_SUPPORT_OPENGL | Wgl.PFD_DOUBLEBUFFER,
            iPixelType = Wgl.PFD_TYPE_RGBA,
            cColorBits = 32,
            cDepthBits = 24,
            cStencilBits = 8,
            iLayerType = Wgl.PFD_MAIN_PLANE
        };

        int pixelFormat = Wgl.ChoosePixelFormat(_hdc, ref pfd);
        if (pixelFormat == 0)
            throw new InvalidOperationException("ChoosePixelFormat failed.");

        if (!Wgl.SetPixelFormat(_hdc, pixelFormat, ref pfd))
            throw new InvalidOperationException("SetPixelFormat failed.");

        _hrc = Wgl.wglCreateContext(_hdc);
        if (_hrc == IntPtr.Zero)
            throw new InvalidOperationException("wglCreateContext failed.");

        if (!Wgl.wglMakeCurrent(_hdc, _hrc))
            throw new InvalidOperationException("wglMakeCurrent failed.");

        GL.LoadFunctions();

        string version = GL.GetString(GL.GL_VERSION);
        Text = $"Rubik's Cube - C# WinForms OpenGL Shader Demo   |   OpenGL {version}";

        GL.Viewport(0, 0, ClientSize.Width, ClientSize.Height);
        GL.ClearColor(0.003f, 0.005f, 0.010f, 1.0f);
        GL.Enable(GL.GL_DEPTH_TEST);
        GL.DepthFunc(GL.GL_LEQUAL);
        GL.Enable(GL.GL_CULL_FACE);
        GL.CullFace(GL.GL_BACK);
        GL.Enable(GL.GL_BLEND);
        GL.BlendFunc(GL.GL_SRC_ALPHA, GL.GL_ONE_MINUS_SRC_ALPHA);
    }

    private void InitScene()
    {
        _program = CreateProgram(
            File.ReadAllText(Files.ShaderVertex),
            File.ReadAllText(Files.ShaderFragment));
        GL.UseProgram(_program);

        _uModel = GL.GetUniformLocation(_program, "uModel");
        _uView = GL.GetUniformLocation(_program, "uView");
        _uProjection = GL.GetUniformLocation(_program, "uProjection");
        _uColor = GL.GetUniformLocation(_program, "uColor");
        _uCameraPos = GL.GetUniformLocation(_program, "uCameraPos");
        _uTime = GL.GetUniformLocation(_program, "uTime");
        _uMaterial = GL.GetUniformLocation(_program, "uMaterial");
        _uAlpha = GL.GetUniformLocation(_program, "uAlpha");
        _uReflection = GL.GetUniformLocation(_program, "uReflection");

        _cubeMesh = Mesh.CreateCube();
        _stickerMesh = Mesh.CreateStickerPlane();
        _floorMesh = Mesh.CreateFloorPlane();
    }

    private void InitRubiksCube()
    {
        _cubies.Clear();

        for (int x = -1; x <= 1; x++)
            for (int y = -1; y <= 1; y++)
                for (int z = -1; z <= 1; z++)
                {
                    var cubie = new Cubie(new Vector3(x, y, z));

                    if (x == 1) cubie.Stickers[Direction.Right] = StickerColor.Red;
                    if (x == -1) cubie.Stickers[Direction.Left] = StickerColor.Orange;
                    if (y == 1) cubie.Stickers[Direction.Up] = StickerColor.White;
                    if (y == -1) cubie.Stickers[Direction.Down] = StickerColor.Yellow;
                    if (z == 1) cubie.Stickers[Direction.Front] = StickerColor.Green;
                    if (z == -1) cubie.Stickers[Direction.Back] = StickerColor.Blue;

                    _cubies.Add(cubie);
                }
    }

    private void ResetCube()
    {
        _moveQueue.Clear();
        _activeMove = null;
        _moveTime = 0;
        InitRubiksCube();
    }

    private void Scramble()
    {
        if (_activeMove != null)
            return;

        _moveQueue.Clear();
        Axis[] axes = { Axis.X, Axis.Y, Axis.Z };
        int[] layers = { -1, 1 };

        for (int i = 0; i < 28; i++)
        {
            Axis axis = axes[_random.Next(axes.Length)];
            int layer = layers[_random.Next(layers.Length)];
            int dir = _random.Next(2) == 0 ? -1 : 1;
            _moveQueue.Enqueue(new Move(axis, layer, dir));
        }
    }

    private void BeginMove(Axis axis, int layer, int dir)
    {
        if (_activeMove != null)
            return;

        _activeMove = new Move(axis, layer, Math.Sign(dir) == 0 ? 1 : Math.Sign(dir));
        _moveTime = 0;
    }

    private void UpdateAnimation()
    {
        double now = _clock.Elapsed.TotalSeconds;
        float dt = (float)(now - _lastSeconds);
        _lastSeconds = now;

        if (_activeMove == null && _moveQueue.Count > 0)
            BeginMove(_moveQueue.Peek().Axis, _moveQueue.Peek().Layer, _moveQueue.Dequeue().Dir);

        if (_activeMove == null)
            return;

        _moveTime += dt;
        if (_moveTime >= MoveDuration)
        {
            ApplyMove(_activeMove.Value);
            _activeMove = null;
            _moveTime = 0;
        }
    }

    private void ApplyMove(Move move)
    {
        foreach (var cubie in _cubies)
        {
            if (!IsInLayer(cubie.Pos, move.Axis, move.Layer))
                continue;

            cubie.Pos = Rotate(cubie.Pos, move.Axis, move.Dir);

            var newStickers = new Dictionary<Direction, StickerColor>();
            foreach (var pair in cubie.Stickers)
                newStickers[Rotate(pair.Key, move.Axis, move.Dir)] = pair.Value;

            cubie.Stickers.Clear();
            foreach (var pair in newStickers)
                cubie.Stickers[pair.Key] = pair.Value;
        }
    }

    private void Render()
    {
        GL.Viewport(0, 0, ClientSize.Width, ClientSize.Height);
        GL.Clear(GL.GL_COLOR_BUFFER_BIT | GL.GL_DEPTH_BUFFER_BIT);
        GL.UseProgram(_program);

        float aspect = ClientSize.Width / Math.Max(1.0f, (float)ClientSize.Height);
        Matrix4 view = Matrix4.Translation(0.0f, 0.0f, -_distance);
        Matrix4 projection = Matrix4.Perspective(0.72f, aspect, 0.05f, 80.0f);
        Matrix4 sceneRotation = Matrix4.Multiply(Matrix4.RotationX(_pitch), Matrix4.RotationY(_yaw));

        GL.UniformMatrix4fv(_uView, view);
        GL.UniformMatrix4fv(_uProjection, projection);
        GL.Uniform3f(_uCameraPos, 0.0f, 0.0f, _distance);
        GL.Uniform1f(_uTime, (float)_clock.Elapsed.TotalSeconds);

        Matrix4 mirror = Matrix4.Multiply(Matrix4.Translation(0.0f, FloorY * 2.0f, 0.0f), Matrix4.Scale(1.0f, -1.0f, 1.0f));

        GL.Disable(GL.GL_CULL_FACE);
        GL.Enable(GL.GL_BLEND);
        GL.BlendFunc(GL.GL_SRC_ALPHA, GL.GL_ONE_MINUS_SRC_ALPHA);
        GL.DepthMask(false);
        DrawCubies(sceneRotation, mirror, reflection: true);

        DrawFloor(sceneRotation);

        GL.DepthMask(true);
        GL.Enable(GL.GL_CULL_FACE);
        GL.CullFace(GL.GL_BACK);
        DrawCubies(sceneRotation, Matrix4.Identity(), reflection: false);

        Wgl.SwapBuffers(_hdc);
    }

    private void DrawCubies(Matrix4 sceneRotation, Matrix4 worldExtra, bool reflection)
    {
        GL.Uniform1f(_uAlpha, reflection ? 0.24f : 1.0f);
        GL.Uniform1i(_uReflection, reflection ? 1 : 0);

        Matrix4 activeLayerRotation = Matrix4.Identity();
        Move? move = _activeMove;

        if (move != null)
        {
            float t = Math.Clamp(_moveTime / MoveDuration, 0f, 1f);
            t = t * t * (3f - 2f * t);
            float angle = move.Value.Dir * t * MathF.PI * 0.5f;
            activeLayerRotation = AxisRotation(move.Value.Axis, angle);
        }

        foreach (var cubie in _cubies)
        {
            bool moving = move != null && IsInLayer(cubie.Pos, move.Value.Axis, move.Value.Layer);
            Matrix4 layerRotation = moving ? activeLayerRotation : Matrix4.Identity();
            System.Numerics.Vector3 pos = cubie.Pos.ToVector3() * CubieSpacing;

            Matrix4 bodyModel = Matrix4.Chain(
                sceneRotation,
                worldExtra,
                layerRotation,
                Matrix4.Translation(pos.X, pos.Y, pos.Z),
                Matrix4.Scale(BodySize, BodySize, BodySize));

            DrawMesh(_cubeMesh, bodyModel, new System.Numerics.Vector3(0.006f, 0.008f, 0.012f), material: 1);

            foreach (var sticker in cubie.Stickers)
            {
                Direction face = sticker.Key;
                System.Numerics.Vector3 normal = face.ToVector3();
                System.Numerics.Vector3 color = GetStickerColor(sticker.Value);

                Matrix4 stickerModel = Matrix4.Chain(
                    sceneRotation,
                    worldExtra,
                    layerRotation,
                    Matrix4.Translation(pos.X, pos.Y, pos.Z),
                    Matrix4.Translation(normal.X * StickerOffset, normal.Y * StickerOffset, normal.Z * StickerOffset),
                    FaceOrientation(face),
                    Matrix4.Scale(StickerSize, StickerSize, StickerSize));

                DrawMesh(_stickerMesh, stickerModel, color, material: 0);
            }
        }
    }

    private void DrawFloor(Matrix4 sceneRotation)
    {
        GL.Uniform1i(_uMaterial, 2);
        GL.Uniform1i(_uReflection, 0);
        GL.Uniform1f(_uAlpha, 0.82f);
        GL.Uniform3f(_uColor, 0.05f, 0.35f, 0.85f);

        Matrix4 floorModel = Matrix4.Chain(
            sceneRotation,
            Matrix4.Translation(0.0f, FloorY, 0.0f),
            Matrix4.Scale(18.0f, 1.0f, 18.0f));

        GL.UniformMatrix4fv(_uModel, floorModel);
        _floorMesh.Draw();
    }

    private void DrawMesh(Mesh mesh, Matrix4 model, System.Numerics.Vector3 color, int material)
    {
        GL.Uniform1i(_uMaterial, material);
        GL.Uniform3f(_uColor, color.X, color.Y, color.Z);
        GL.UniformMatrix4fv(_uModel, model);
        mesh.Draw();
    }

    private static Matrix4 AxisRotation(Axis axis, float angle)
    {
        return axis switch
        {
            Axis.X => Matrix4.RotationX(angle),
            Axis.Y => Matrix4.RotationY(angle),
            Axis.Z => Matrix4.RotationZ(angle),
            _ => Matrix4.Identity()
        };
    }

    private static Matrix4 FaceOrientation(Direction face)
    {
        return face switch
        {
            Direction.Front => Matrix4.Identity(),
            Direction.Back => Matrix4.RotationY(MathF.PI),
            Direction.Right => Matrix4.RotationY(MathF.PI * 0.5f),
            Direction.Left => Matrix4.RotationY(-MathF.PI * 0.5f),
            Direction.Up => Matrix4.RotationX(-MathF.PI * 0.5f),
            Direction.Down => Matrix4.RotationX(MathF.PI * 0.5f),
            _ => Matrix4.Identity()
        };
    }

    private static bool IsInLayer(Vector3 pos, Axis axis, int layer)
    {
        return axis switch
        {
            Axis.X => pos.X == layer,
            Axis.Y => pos.Y == layer,
            Axis.Z => pos.Z == layer,
            _ => false
        };
    }

    private static Vector3 Rotate(Vector3 p, Axis axis, int dir)
    {
        dir = Math.Sign(dir);
        if (dir == 0) dir = 1;

        return axis switch
        {
            Axis.X => dir > 0 ? new Vector3(p.X, -p.Z, p.Y) : new Vector3(p.X, p.Z, -p.Y),
            Axis.Y => dir > 0 ? new Vector3(p.Z, p.Y, -p.X) : new Vector3(-p.Z, p.Y, p.X),
            Axis.Z => dir > 0 ? new Vector3(-p.Y, p.X, p.Z) : new Vector3(p.Y, -p.X, p.Z),
            _ => p
        };
    }

    private static Direction Rotate(Direction d, Axis axis, int dir)
    {
        return Rotate(d.ToVec3i(), axis, dir).ToDir();
    }

    private static System.Numerics.Vector3 GetStickerColor(StickerColor color)
    {
        return color switch
        {
            StickerColor.White => new System.Numerics.Vector3(0.96f, 0.96f, 0.88f),
            StickerColor.Yellow => new System.Numerics.Vector3(1.00f, 0.82f, 0.04f),
            StickerColor.Red => new System.Numerics.Vector3(0.95f, 0.04f, 0.05f),
            StickerColor.Orange => new System.Numerics.Vector3(1.00f, 0.38f, 0.03f),
            StickerColor.Green => new System.Numerics.Vector3(0.02f, 0.74f, 0.28f),
            StickerColor.Blue => new System.Numerics.Vector3(0.02f, 0.30f, 1.00f),
            _ => System.Numerics.Vector3.One
        };
    }

    private static uint CreateProgram(string vertexSource, string fragmentSource)
    {
        uint vs = CompileShader(GL.GL_VERTEX_SHADER, vertexSource);
        uint fs = CompileShader(GL.GL_FRAGMENT_SHADER, fragmentSource);

        uint program = GL.CreateProgram();
        GL.AttachShader(program, vs);
        GL.AttachShader(program, fs);
        GL.LinkProgram(program);

        GL.GetProgramiv(program, GL.GL_LINK_STATUS, out int ok);
        if (ok == 0)
        {
            string log = GetProgramLog(program);
            throw new Exception("Program link failed:\n" + log);
        }

        GL.DeleteShader(vs);
        GL.DeleteShader(fs);
        return program;
    }

    private static uint CompileShader(uint type, string source)
    {
        uint shader = GL.CreateShader(type);
        GL.ShaderSource(shader, source);
        GL.CompileShader(shader);

        GL.GetShaderiv(shader, GL.GL_COMPILE_STATUS, out int ok);
        if (ok == 0)
        {
            string log = GetShaderLog(shader);
            throw new Exception("Shader compilation failed:\n" + log);
        }

        return shader;
    }

    private static string GetShaderLog(uint shader)
    {
        GL.GetShaderiv(shader, GL.GL_INFO_LOG_LENGTH, out int len);
        if (len <= 1) return string.Empty;

        IntPtr buffer = Marshal.AllocHGlobal(len);
        try
        {
            GL.GetShaderInfoLog(shader, len, out _, buffer);
            return Marshal.PtrToStringAnsi(buffer) ?? string.Empty;
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    private static string GetProgramLog(uint program)
    {
        GL.GetProgramiv(program, GL.GL_INFO_LOG_LENGTH, out int len);
        if (len <= 1) return string.Empty;

        IntPtr buffer = Marshal.AllocHGlobal(len);
        try
        {
            GL.GetProgramInfoLog(program, len, out _, buffer);
            return Marshal.PtrToStringAnsi(buffer) ?? string.Empty;
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }
}
