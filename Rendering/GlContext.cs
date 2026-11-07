using System.Runtime.InteropServices;

namespace RubiksCube.Rendering;

// An OpenGL 4.0 core profile context on a window, with 4x MSAA when the driver offers
// it and vsync. Created current; throws InvalidOperationException when OpenGL 4.0 is
// not available.
internal sealed unsafe class GlContext : IDisposable
{
    // WGL_ARB_pixel_format, WGL_ARB_multisample, WGL_ARB_create_context(_profile).
    private const int WGL_DRAW_TO_WINDOW_ARB = 0x2001;
    private const int WGL_ACCELERATION_ARB = 0x2003;
    private const int WGL_SUPPORT_OPENGL_ARB = 0x2010;
    private const int WGL_DOUBLE_BUFFER_ARB = 0x2011;
    private const int WGL_PIXEL_TYPE_ARB = 0x2013;
    private const int WGL_COLOR_BITS_ARB = 0x2014;
    private const int WGL_DEPTH_BITS_ARB = 0x2022;
    private const int WGL_STENCIL_BITS_ARB = 0x2023;
    private const int WGL_FULL_ACCELERATION_ARB = 0x2027;
    private const int WGL_TYPE_RGBA_ARB = 0x202B;
    private const int WGL_SAMPLE_BUFFERS_ARB = 0x2041;
    private const int WGL_SAMPLES_ARB = 0x2042;
    private const int WGL_CONTEXT_MAJOR_VERSION_ARB = 0x2091;
    private const int WGL_CONTEXT_MINOR_VERSION_ARB = 0x2092;
    private const int WGL_CONTEXT_FLAGS_ARB = 0x2094;
    private const int WGL_CONTEXT_PROFILE_MASK_ARB = 0x9126;
    private const int WGL_CONTEXT_CORE_PROFILE_BIT_ARB = 0x0001;
    private const int WGL_CONTEXT_FORWARD_COMPATIBLE_BIT_ARB = 0x0002;

    private static readonly nint OpenGl32 = NativeLibrary.Load("opengl32.dll");

    private readonly nint _window;
    private nint _dc;
    private nint _context;

    public GlContext(nint window)
    {
        _window = window;
        try
        {
            (nint choosePixelFormat, nint createContextAttribs) = LoadWglExtensions();

            _dc = Native.GetDC(window);
            if (_dc == 0)
                throw new InvalidOperationException("The window has no device context.");
            SetPixelFormat(_dc, (delegate* unmanaged[Stdcall]<nint, int*, float*, uint, int*, uint*, int>)choosePixelFormat);

            _context = CreateContext(_dc, (delegate* unmanaged[Stdcall]<nint, nint, int*, nint>)createContextAttribs);
            if (_context == 0 || !Native.wglMakeCurrent(_dc, _context))
                throw new InvalidOperationException("The graphics driver does not support OpenGL 4.0.");

            Gl.Load(GetProcAddress);
            Version = Gl.GetString(Gl.GL_VERSION);
            if (Gl.GetInteger(Gl.GL_MAJOR_VERSION) < 4)
                throw new InvalidOperationException($"OpenGL 4.0 is required; the graphics driver offers {Version}.");

            // Vsync. The function belongs to the context, so it is looked up only now.
            var swapInterval = (delegate* unmanaged[Stdcall]<int, int>)Native.wglGetProcAddress("wglSwapIntervalEXT");
            if (swapInterval != null)
                swapInterval(1);
        }
        catch
        {
            Dispose();
            throw;
        }
    }

    public string Version { get; } = "";

    public void SwapBuffers() => Native.SwapBuffers(_dc);

    public void Dispose()
    {
        if (_context != 0)
        {
            Native.wglMakeCurrent(0, 0);
            Native.wglDeleteContext(_context);
            _context = 0;
        }
        if (_dc != 0)
        {
            _ = Native.ReleaseDC(_window, _dc);
            _dc = 0;
        }
    }

    // The WGL extension functions can only be looked up with a context current, and a
    // window's pixel format can be set only once, so this takes a throwaway window with
    // a legacy context. Missing functions come back as 0.
    private static (nint ChoosePixelFormat, nint CreateContextAttribs) LoadWglExtensions()
    {
        var window = new NativeWindow();
        window.CreateHandle(new CreateParams());
        try
        {
            nint dc = Native.GetDC(window.Handle);
            try
            {
                Native.PixelFormatDescriptor pfd = BasicPixelFormat();
                if (!Native.SetPixelFormat(dc, Native.ChoosePixelFormat(dc, in pfd), in pfd))
                    return default;
                nint context = Native.wglCreateContext(dc);
                if (context == 0)
                    return default;
                try
                {
                    if (!Native.wglMakeCurrent(dc, context))
                        return default;
                    var functions = (Native.wglGetProcAddress("wglChoosePixelFormatARB"),
                                     Native.wglGetProcAddress("wglCreateContextAttribsARB"));
                    Native.wglMakeCurrent(0, 0);
                    return functions;
                }
                finally
                {
                    Native.wglDeleteContext(context);
                }
            }
            finally
            {
                _ = Native.ReleaseDC(window.Handle, dc);
            }
        }
        finally
        {
            window.DestroyHandle();
        }
    }

    private static Native.PixelFormatDescriptor BasicPixelFormat() => new()
    {
        nSize = (ushort)sizeof(Native.PixelFormatDescriptor),
        nVersion = 1,
        dwFlags = Native.PFD_DRAW_TO_WINDOW | Native.PFD_SUPPORT_OPENGL | Native.PFD_DOUBLEBUFFER,
        iPixelType = Native.PFD_TYPE_RGBA,
        cColorBits = 32,
        cDepthBits = 24,
        cStencilBits = 8,
    };

    // RGBA with 4x MSAA through WGL_ARB_pixel_format; without the extension or such a
    // format, the basic format without multisampling.
    private static void SetPixelFormat(nint dc, delegate* unmanaged[Stdcall]<nint, int*, float*, uint, int*, uint*, int> choosePixelFormat)
    {
        if (choosePixelFormat != null)
        {
            int* attributes = stackalloc int[]
            {
                WGL_DRAW_TO_WINDOW_ARB, 1,
                WGL_SUPPORT_OPENGL_ARB, 1,
                WGL_DOUBLE_BUFFER_ARB, 1,
                WGL_ACCELERATION_ARB, WGL_FULL_ACCELERATION_ARB,
                WGL_PIXEL_TYPE_ARB, WGL_TYPE_RGBA_ARB,
                WGL_COLOR_BITS_ARB, 32,
                WGL_DEPTH_BITS_ARB, 24,
                WGL_STENCIL_BITS_ARB, 8,
                WGL_SAMPLE_BUFFERS_ARB, 1,
                WGL_SAMPLES_ARB, 4,
                0,
            };
            int format;
            uint count;
            if (choosePixelFormat(dc, attributes, null, 1, &format, &count) != 0 && count > 0
                && Native.DescribePixelFormat(dc, format, (uint)sizeof(Native.PixelFormatDescriptor), out var described) != 0
                && Native.SetPixelFormat(dc, format, in described))
                return;
        }

        Native.PixelFormatDescriptor pfd = BasicPixelFormat();
        int basic = Native.ChoosePixelFormat(dc, in pfd);
        if (basic == 0 || !Native.SetPixelFormat(dc, basic, in pfd))
            throw new InvalidOperationException("No pixel format for OpenGL is available.");
    }

    private static nint CreateContext(nint dc, delegate* unmanaged[Stdcall]<nint, nint, int*, nint> createContextAttribs)
    {
        if (createContextAttribs == null)   // a very old driver: its default context, the version check decides
            return Native.wglCreateContext(dc);

        int* attributes = stackalloc int[]
        {
            WGL_CONTEXT_MAJOR_VERSION_ARB, 4,
            WGL_CONTEXT_MINOR_VERSION_ARB, 0,
            WGL_CONTEXT_PROFILE_MASK_ARB, WGL_CONTEXT_CORE_PROFILE_BIT_ARB,
            WGL_CONTEXT_FLAGS_ARB, WGL_CONTEXT_FORWARD_COMPATIBLE_BIT_ARB,
            0,
        };
        return createContextAttribs(dc, 0, attributes);
    }

    // wglGetProcAddress has the functions newer than OpenGL 1.1, opengl32.dll exports the
    // rest. Some drivers return small sentinel values instead of 0 for unknown names.
    private static nint GetProcAddress(string name)
    {
        nint address = Native.wglGetProcAddress(name);

        if (address is 0 or 1 or 2 or 3 or -1)
            return NativeLibrary.TryGetExport(OpenGl32, name, out nint export) ? export : 0;

        return address;
    }
}
