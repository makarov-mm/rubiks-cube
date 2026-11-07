using System.Runtime.InteropServices;

namespace RubiksCube.Rendering;

// The Win32 functions behind the OpenGL context and the render loop.
internal static partial class Native
{
    public const uint PFD_DOUBLEBUFFER = 0x00000001;
    public const uint PFD_DRAW_TO_WINDOW = 0x00000004;
    public const uint PFD_SUPPORT_OPENGL = 0x00000020;
    public const byte PFD_TYPE_RGBA = 0;
    private const uint PM_NOREMOVE = 0x0000;

    [StructLayout(LayoutKind.Sequential)]
    public struct PixelFormatDescriptor
    {
        public ushort nSize;
        public ushort nVersion;
        public uint dwFlags;
        public byte iPixelType;
        public byte cColorBits;
        public byte cRedBits;
        public byte cRedShift;
        public byte cGreenBits;
        public byte cGreenShift;
        public byte cBlueBits;
        public byte cBlueShift;
        public byte cAlphaBits;
        public byte cAlphaShift;
        public byte cAccumBits;
        public byte cAccumRedBits;
        public byte cAccumGreenBits;
        public byte cAccumBlueBits;
        public byte cAccumAlphaBits;
        public byte cDepthBits;
        public byte cStencilBits;
        public byte cAuxBuffers;
        public byte iLayerType;
        public byte bReserved;
        public uint dwLayerMask;
        public uint dwVisibleMask;
        public uint dwDamageMask;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Msg
    {
        public nint hwnd;
        public uint message;
        public nuint wParam;
        public nint lParam;
        public uint time;
        public int ptX;
        public int ptY;
    }

    [LibraryImport("user32.dll")]
    public static partial nint GetDC(nint hwnd);

    [LibraryImport("user32.dll")]
    public static partial int ReleaseDC(nint hwnd, nint hdc);

    [LibraryImport("gdi32.dll")]
    public static partial int ChoosePixelFormat(nint hdc, in PixelFormatDescriptor descriptor);

    [LibraryImport("gdi32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool SetPixelFormat(nint hdc, int format, in PixelFormatDescriptor descriptor);

    [LibraryImport("gdi32.dll")]
    public static partial int DescribePixelFormat(nint hdc, int format, uint size, out PixelFormatDescriptor descriptor);

    [LibraryImport("gdi32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool SwapBuffers(nint hdc);

    [LibraryImport("opengl32.dll")]
    public static partial nint wglCreateContext(nint hdc);

    [LibraryImport("opengl32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool wglMakeCurrent(nint hdc, nint context);

    [LibraryImport("opengl32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool wglDeleteContext(nint context);

    [LibraryImport("opengl32.dll", StringMarshalling = StringMarshalling.Utf8)]
    public static partial nint wglGetProcAddress(string name);

    [LibraryImport("user32.dll", EntryPoint = "PeekMessageW")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool PeekMessage(out Msg message, nint hwnd, uint filterMin, uint filterMax, uint remove);

    // Whether the thread's message queue has anything waiting.
    public static bool HasPendingMessages() => PeekMessage(out _, 0, 0, 0, PM_NOREMOVE);
}
