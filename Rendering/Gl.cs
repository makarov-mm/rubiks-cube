using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;

namespace RubiksCube.Rendering;

// The OpenGL functions the renderer uses, called through unmanaged function pointers
// (no delegates, no marshalling). Load() fills them in once a context is current.
internal static unsafe class Gl
{
    public const uint GL_DEPTH_BUFFER_BIT = 0x00000100;
    public const uint GL_COLOR_BUFFER_BIT = 0x00004000;
    public const uint GL_TRIANGLES = 0x0004;
    public const uint GL_LEQUAL = 0x0203;
    public const uint GL_SRC_ALPHA = 0x0302;
    public const uint GL_ONE_MINUS_SRC_ALPHA = 0x0303;
    public const uint GL_BACK = 0x0405;
    public const uint GL_CULL_FACE = 0x0B44;
    public const uint GL_DEPTH_TEST = 0x0B71;
    public const uint GL_BLEND = 0x0BE2;
    public const uint GL_FLOAT = 0x1406;
    public const uint GL_VERSION = 0x1F02;
    public const uint GL_MAJOR_VERSION = 0x821B;
    public const uint GL_ARRAY_BUFFER = 0x8892;
    public const uint GL_STATIC_DRAW = 0x88E4;
    public const uint GL_FRAGMENT_SHADER = 0x8B30;
    public const uint GL_VERTEX_SHADER = 0x8B31;
    public const uint GL_COMPILE_STATUS = 0x8B81;
    public const uint GL_LINK_STATUS = 0x8B82;
    public const uint GL_INFO_LOG_LENGTH = 0x8B84;

    private static delegate* unmanaged[Stdcall]<uint, byte*> s_getString;
    private static delegate* unmanaged[Stdcall]<uint, int*, void> s_getIntegerv;
    private static delegate* unmanaged[Stdcall]<int, int, int, int, void> s_viewport;
    private static delegate* unmanaged[Stdcall]<float, float, float, float, void> s_clearColor;
    private static delegate* unmanaged[Stdcall]<uint, void> s_clear;
    private static delegate* unmanaged[Stdcall]<uint, void> s_enable;
    private static delegate* unmanaged[Stdcall]<uint, void> s_disable;
    private static delegate* unmanaged[Stdcall]<uint, void> s_cullFace;
    private static delegate* unmanaged[Stdcall]<uint, uint, void> s_blendFunc;
    private static delegate* unmanaged[Stdcall]<uint, void> s_depthFunc;
    private static delegate* unmanaged[Stdcall]<byte, void> s_depthMask;
    private static delegate* unmanaged[Stdcall]<uint, int, int, void> s_drawArrays;

    private static delegate* unmanaged[Stdcall]<uint, uint> s_createShader;
    private static delegate* unmanaged[Stdcall]<uint, int, byte**, int*, void> s_shaderSource;
    private static delegate* unmanaged[Stdcall]<uint, void> s_compileShader;
    private static delegate* unmanaged[Stdcall]<uint, uint, int*, void> s_getShaderiv;
    private static delegate* unmanaged[Stdcall]<uint, int, int*, byte*, void> s_getShaderInfoLog;
    private static delegate* unmanaged[Stdcall]<uint, void> s_deleteShader;
    private static delegate* unmanaged[Stdcall]<uint> s_createProgram;
    private static delegate* unmanaged[Stdcall]<uint, uint, void> s_attachShader;
    private static delegate* unmanaged[Stdcall]<uint, void> s_linkProgram;
    private static delegate* unmanaged[Stdcall]<uint, uint, int*, void> s_getProgramiv;
    private static delegate* unmanaged[Stdcall]<uint, int, int*, byte*, void> s_getProgramInfoLog;
    private static delegate* unmanaged[Stdcall]<uint, void> s_useProgram;
    private static delegate* unmanaged[Stdcall]<uint, void> s_deleteProgram;
    private static delegate* unmanaged[Stdcall]<uint, byte*, int> s_getUniformLocation;
    private static delegate* unmanaged[Stdcall]<int, int, byte, float*, void> s_uniformMatrix4fv;
    private static delegate* unmanaged[Stdcall]<int, float, float, float, void> s_uniform3f;
    private static delegate* unmanaged[Stdcall]<int, float, void> s_uniform1f;
    private static delegate* unmanaged[Stdcall]<int, int, void> s_uniform1i;

    private static delegate* unmanaged[Stdcall]<int, uint*, void> s_genVertexArrays;
    private static delegate* unmanaged[Stdcall]<uint, void> s_bindVertexArray;
    private static delegate* unmanaged[Stdcall]<int, uint*, void> s_deleteVertexArrays;
    private static delegate* unmanaged[Stdcall]<int, uint*, void> s_genBuffers;
    private static delegate* unmanaged[Stdcall]<uint, uint, void> s_bindBuffer;
    private static delegate* unmanaged[Stdcall]<uint, nint, void*, uint, void> s_bufferData;
    private static delegate* unmanaged[Stdcall]<int, uint*, void> s_deleteBuffers;
    private static delegate* unmanaged[Stdcall]<uint, void> s_enableVertexAttribArray;
    private static delegate* unmanaged[Stdcall]<uint, int, uint, byte, int, void*, void> s_vertexAttribPointer;

    // getProcAddress returns the address of an OpenGL function by name, 0 if there is none.
    public static void Load(Func<string, nint> getProcAddress)
    {
        nint Get(string name)
        {
            nint address = getProcAddress(name);
            return address != 0 ? address
                : throw new InvalidOperationException($"The graphics driver lacks {name} (OpenGL 4.0 is required).");
        }

        s_getString = (delegate* unmanaged[Stdcall]<uint, byte*>)Get("glGetString");
        s_getIntegerv = (delegate* unmanaged[Stdcall]<uint, int*, void>)Get("glGetIntegerv");
        s_viewport = (delegate* unmanaged[Stdcall]<int, int, int, int, void>)Get("glViewport");
        s_clearColor = (delegate* unmanaged[Stdcall]<float, float, float, float, void>)Get("glClearColor");
        s_clear = (delegate* unmanaged[Stdcall]<uint, void>)Get("glClear");
        s_enable = (delegate* unmanaged[Stdcall]<uint, void>)Get("glEnable");
        s_disable = (delegate* unmanaged[Stdcall]<uint, void>)Get("glDisable");
        s_cullFace = (delegate* unmanaged[Stdcall]<uint, void>)Get("glCullFace");
        s_blendFunc = (delegate* unmanaged[Stdcall]<uint, uint, void>)Get("glBlendFunc");
        s_depthFunc = (delegate* unmanaged[Stdcall]<uint, void>)Get("glDepthFunc");
        s_depthMask = (delegate* unmanaged[Stdcall]<byte, void>)Get("glDepthMask");
        s_drawArrays = (delegate* unmanaged[Stdcall]<uint, int, int, void>)Get("glDrawArrays");

        s_createShader = (delegate* unmanaged[Stdcall]<uint, uint>)Get("glCreateShader");
        s_shaderSource = (delegate* unmanaged[Stdcall]<uint, int, byte**, int*, void>)Get("glShaderSource");
        s_compileShader = (delegate* unmanaged[Stdcall]<uint, void>)Get("glCompileShader");
        s_getShaderiv = (delegate* unmanaged[Stdcall]<uint, uint, int*, void>)Get("glGetShaderiv");
        s_getShaderInfoLog = (delegate* unmanaged[Stdcall]<uint, int, int*, byte*, void>)Get("glGetShaderInfoLog");
        s_deleteShader = (delegate* unmanaged[Stdcall]<uint, void>)Get("glDeleteShader");
        s_createProgram = (delegate* unmanaged[Stdcall]<uint>)Get("glCreateProgram");
        s_attachShader = (delegate* unmanaged[Stdcall]<uint, uint, void>)Get("glAttachShader");
        s_linkProgram = (delegate* unmanaged[Stdcall]<uint, void>)Get("glLinkProgram");
        s_getProgramiv = (delegate* unmanaged[Stdcall]<uint, uint, int*, void>)Get("glGetProgramiv");
        s_getProgramInfoLog = (delegate* unmanaged[Stdcall]<uint, int, int*, byte*, void>)Get("glGetProgramInfoLog");
        s_useProgram = (delegate* unmanaged[Stdcall]<uint, void>)Get("glUseProgram");
        s_deleteProgram = (delegate* unmanaged[Stdcall]<uint, void>)Get("glDeleteProgram");
        s_getUniformLocation = (delegate* unmanaged[Stdcall]<uint, byte*, int>)Get("glGetUniformLocation");
        s_uniformMatrix4fv = (delegate* unmanaged[Stdcall]<int, int, byte, float*, void>)Get("glUniformMatrix4fv");
        s_uniform3f = (delegate* unmanaged[Stdcall]<int, float, float, float, void>)Get("glUniform3f");
        s_uniform1f = (delegate* unmanaged[Stdcall]<int, float, void>)Get("glUniform1f");
        s_uniform1i = (delegate* unmanaged[Stdcall]<int, int, void>)Get("glUniform1i");

        s_genVertexArrays = (delegate* unmanaged[Stdcall]<int, uint*, void>)Get("glGenVertexArrays");
        s_bindVertexArray = (delegate* unmanaged[Stdcall]<uint, void>)Get("glBindVertexArray");
        s_deleteVertexArrays = (delegate* unmanaged[Stdcall]<int, uint*, void>)Get("glDeleteVertexArrays");
        s_genBuffers = (delegate* unmanaged[Stdcall]<int, uint*, void>)Get("glGenBuffers");
        s_bindBuffer = (delegate* unmanaged[Stdcall]<uint, uint, void>)Get("glBindBuffer");
        s_bufferData = (delegate* unmanaged[Stdcall]<uint, nint, void*, uint, void>)Get("glBufferData");
        s_deleteBuffers = (delegate* unmanaged[Stdcall]<int, uint*, void>)Get("glDeleteBuffers");
        s_enableVertexAttribArray = (delegate* unmanaged[Stdcall]<uint, void>)Get("glEnableVertexAttribArray");
        s_vertexAttribPointer = (delegate* unmanaged[Stdcall]<uint, int, uint, byte, int, void*, void>)Get("glVertexAttribPointer");
    }

    // ---- state and drawing --------------------------------------------------------

    public static string GetString(uint name) => Marshal.PtrToStringUTF8((nint)s_getString(name)) ?? "unknown";

    public static int GetInteger(uint name)
    {
        int value = 0;
        s_getIntegerv(name, &value);
        return value;
    }

    public static void Viewport(int x, int y, int width, int height) => s_viewport(x, y, width, height);
    public static void ClearColor(float r, float g, float b, float a) => s_clearColor(r, g, b, a);
    public static void Clear(uint mask) => s_clear(mask);
    public static void Enable(uint capability) => s_enable(capability);
    public static void Disable(uint capability) => s_disable(capability);
    public static void CullFace(uint mode) => s_cullFace(mode);
    public static void BlendFunc(uint source, uint destination) => s_blendFunc(source, destination);
    public static void DepthFunc(uint function) => s_depthFunc(function);
    public static void DepthMask(bool write) => s_depthMask(write ? (byte)1 : (byte)0);
    public static void DrawArrays(uint mode, int first, int count) => s_drawArrays(mode, first, count);

    // ---- shaders --------------------------------------------------------------------

    public static uint CreateShader(uint type) => s_createShader(type);

    public static void ShaderSource(uint shader, string source)
    {
        byte[] text = Encoding.UTF8.GetBytes(source);
        fixed (byte* pointer = text)
        {
            byte* first = pointer;
            int length = text.Length;
            s_shaderSource(shader, 1, &first, &length);
        }
    }

    public static void CompileShader(uint shader) => s_compileShader(shader);
    public static void DeleteShader(uint shader) => s_deleteShader(shader);

    public static int GetShader(uint shader, uint parameter)
    {
        int value = 0;
        s_getShaderiv(shader, parameter, &value);
        return value;
    }

    public static string GetShaderInfoLog(uint shader)
    {
        byte[] log = new byte[Math.Max(GetShader(shader, GL_INFO_LOG_LENGTH), 1)];
        fixed (byte* pointer = log)
            s_getShaderInfoLog(shader, log.Length, null, pointer);
        return Decode(log);
    }

    public static uint CreateProgram() => s_createProgram();
    public static void AttachShader(uint program, uint shader) => s_attachShader(program, shader);
    public static void LinkProgram(uint program) => s_linkProgram(program);
    public static void UseProgram(uint program) => s_useProgram(program);
    public static void DeleteProgram(uint program) => s_deleteProgram(program);

    public static int GetProgram(uint program, uint parameter)
    {
        int value = 0;
        s_getProgramiv(program, parameter, &value);
        return value;
    }

    public static string GetProgramInfoLog(uint program)
    {
        byte[] log = new byte[Math.Max(GetProgram(program, GL_INFO_LOG_LENGTH), 1)];
        fixed (byte* pointer = log)
            s_getProgramInfoLog(program, log.Length, null, pointer);
        return Decode(log);
    }

    public static int GetUniformLocation(uint program, string name)
    {
        byte[] text = Encoding.UTF8.GetBytes(name + '\0');
        fixed (byte* pointer = text)
            return s_getUniformLocation(program, pointer);
    }

    // Location -1 (a uniform the compiler optimized away) is ignored, as OpenGL specifies.
    public static void Uniform1(int location, int value) => s_uniform1i(location, value);
    public static void Uniform1(int location, float value) => s_uniform1f(location, value);
    public static void Uniform3(int location, Vector3 value) => s_uniform3f(location, value.X, value.Y, value.Z);

    // Matrix4x4 keeps the translation in M41..M43, which is OpenGL's column-major layout.
    public static void UniformMatrix4(int location, in Matrix4x4 value)
    {
        fixed (float* elements = &value.M11)
            s_uniformMatrix4fv(location, 1, 0, elements);
    }

    // ---- buffers --------------------------------------------------------------------

    public static uint GenVertexArray()
    {
        uint name = 0;
        s_genVertexArrays(1, &name);
        return name;
    }

    public static void BindVertexArray(uint array) => s_bindVertexArray(array);
    public static void DeleteVertexArray(uint array) => s_deleteVertexArrays(1, &array);

    public static uint GenBuffer()
    {
        uint name = 0;
        s_genBuffers(1, &name);
        return name;
    }

    public static void BindBuffer(uint target, uint buffer) => s_bindBuffer(target, buffer);
    public static void DeleteBuffer(uint buffer) => s_deleteBuffers(1, &buffer);

    public static void BufferData<T>(uint target, ReadOnlySpan<T> data, uint usage) where T : unmanaged
    {
        fixed (T* pointer = data)
            s_bufferData(target, (nint)data.Length * sizeof(T), pointer, usage);
    }

    public static void EnableVertexAttribArray(uint index) => s_enableVertexAttribArray(index);

    // A float attribute with `size` components, `offset` bytes into each vertex.
    public static void VertexAttribPointer(uint index, int size, int stride, nint offset) =>
        s_vertexAttribPointer(index, size, GL_FLOAT, 0, stride, (void*)offset);

    // An info log: null-terminated text.
    private static string Decode(byte[] log)
    {
        int end = Array.IndexOf(log, (byte)0);
        return Encoding.UTF8.GetString(log, 0, end < 0 ? log.Length : end).TrimEnd();
    }
}
