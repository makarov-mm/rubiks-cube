using System.Diagnostics;
using RubiksCube.Puzzle;
using RubiksCube.Rendering;

namespace RubiksCube;

internal sealed class RubiksForm : Form
{
    private const float DragRadiansPerPixel = 0.008f;   // at 96 dpi
    private const float ZoomPerWheelNotch = 0.90f;

    private readonly Cube _cube = new();
    private readonly Stopwatch _clock = Stopwatch.StartNew();
    private Camera _camera = Camera.Default;
    private TimeSpan _lastFrame;
    private bool _dragging;
    private Point _lastMouse;

    private GlContext? _gl;
    private CubeRenderer? _renderer;

    public RubiksForm()
    {
        Text = "Rubik's Cube - C# WinForms OpenGL Shader Demo";
        ClientSize = LogicalToDeviceUnits(new Size(1100, 850));
        MinimumSize = LogicalToDeviceUnits(new Size(640, 480));
        // OpenGL paints the whole client area, and again while the window is resized.
        SetStyle(ControlStyles.Opaque | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
    }

    protected override CreateParams CreateParams
    {
        get
        {
            const int CS_OWNDC = 0x0020;   // OpenGL keeps using the window's device context
            CreateParams cp = base.CreateParams;
            cp.ClassStyle |= CS_OWNDC;
            return cp;
        }
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        try
        {
            _gl = new GlContext(Handle);
            _renderer = new CubeRenderer();
        }
        catch (Exception error)
        {
            _gl?.Dispose();
            _gl = null;
            MessageBox.Show(this, error.Message, "Rubik's Cube", MessageBoxButtons.OK, MessageBoxIcon.Error);
            BeginInvoke(Close);
            return;
        }

        Text = $"Rubik's Cube - C# WinForms OpenGL Shader Demo   |   OpenGL {_gl.Version}";
        _lastFrame = _clock.Elapsed;
        Application.Idle += OnIdle;
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        Application.Idle -= OnIdle;
        _renderer?.Dispose();   // its OpenGL objects go while the context is still current
        _renderer = null;
        _gl?.Dispose();
        _gl = null;
        base.OnFormClosed(e);
    }

    // Renders frame after frame while there are no messages to handle; vsync paces it.
    private void OnIdle(object? sender, EventArgs e)
    {
        while (WindowState != FormWindowState.Minimized && !Native.HasPendingMessages())
            RenderFrame();
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        // OpenGL owns the whole client area.
    }

    protected override void OnPaint(PaintEventArgs e) => RenderFrame();

    private void RenderFrame()
    {
        if (_gl is null || _renderer is null || ClientSize.Width <= 0 || ClientSize.Height <= 0)
            return;

        TimeSpan now = _clock.Elapsed;
        _cube.Update((float)(now - _lastFrame).TotalSeconds);
        _lastFrame = now;

        _renderer.Render(_cube, _camera, ClientSize.Width, ClientSize.Height, (float)now.TotalSeconds, DeviceDpi / 96f);
        _gl.SwapBuffers();
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            _dragging = true;
            _lastMouse = e.Location;
            Capture = true;
        }
        base.OnMouseDown(e);
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
            Capture = false;   // OnMouseCaptureChanged ends the drag
        base.OnMouseUp(e);
    }

    // Also when another window takes the mouse away in the middle of a drag.
    protected override void OnMouseCaptureChanged(EventArgs e)
    {
        _dragging = false;
        base.OnMouseCaptureChanged(e);
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        if (_dragging)
        {
            float radiansPerPixel = DragRadiansPerPixel * 96f / DeviceDpi;
            _camera = _camera.Orbit((e.X - _lastMouse.X) * radiansPerPixel, (e.Y - _lastMouse.Y) * radiansPerPixel);
            _lastMouse = e.Location;
        }
        base.OnMouseMove(e);
    }

    protected override void OnMouseWheel(MouseEventArgs e)
    {
        _camera = _camera.Zoom(MathF.Pow(ZoomPerWheelNotch, e.Delta / (float)SystemInformation.MouseWheelScrollDelta));
        base.OnMouseWheel(e);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        Face? face = e.KeyCode switch
        {
            Keys.R => Face.Right,
            Keys.L => Face.Left,
            Keys.U => Face.Up,
            Keys.D => Face.Down,
            Keys.F => Face.Front,
            Keys.B => Face.Back,
            _ => null,
        };

        if (e is { KeyCode: Keys.R, Control: true })
            _cube.Reset();
        else if (face is { } turned)
            _cube.Turn(Puzzle.Move.Of(turned, clockwise: !e.Shift));
        else switch (e.KeyCode)
        {
            case Keys.S:
                _cube.Scramble(Random.Shared);
                break;
            case Keys.Home:
                _camera = Camera.Default;
                break;
            case Keys.Escape:
                Close();
                break;
        }

        base.OnKeyDown(e);
    }
}
