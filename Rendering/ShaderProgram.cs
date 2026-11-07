namespace RubiksCube.Rendering;

internal sealed class ShaderProgram : IDisposable
{
    // Throws InvalidOperationException with the driver's log when a shader doesn't build.
    public ShaderProgram(string vertexSource, string fragmentSource)
    {
        uint vertexShader = Compile(Gl.GL_VERTEX_SHADER, vertexSource, "vertex");
        uint fragmentShader;
        try
        {
            fragmentShader = Compile(Gl.GL_FRAGMENT_SHADER, fragmentSource, "fragment");
        }
        catch
        {
            Gl.DeleteShader(vertexShader);
            throw;
        }

        Handle = Gl.CreateProgram();
        Gl.AttachShader(Handle, vertexShader);
        Gl.AttachShader(Handle, fragmentShader);
        Gl.LinkProgram(Handle);
        Gl.DeleteShader(vertexShader);     // released together with the program
        Gl.DeleteShader(fragmentShader);

        if (Gl.GetProgram(Handle, Gl.GL_LINK_STATUS) == 0)
        {
            string log = Gl.GetProgramInfoLog(Handle);
            Gl.DeleteProgram(Handle);
            throw new InvalidOperationException($"The shader program failed to link:\n{log}");
        }
    }

    public uint Handle { get; }

    public int Uniform(string name) => Gl.GetUniformLocation(Handle, name);

    public void Use() => Gl.UseProgram(Handle);

    public void Dispose() => Gl.DeleteProgram(Handle);

    private static uint Compile(uint type, string source, string kind)
    {
        uint shader = Gl.CreateShader(type);
        Gl.ShaderSource(shader, source);
        Gl.CompileShader(shader);
        if (Gl.GetShader(shader, Gl.GL_COMPILE_STATUS) != 0)
            return shader;

        string log = Gl.GetShaderInfoLog(shader);
        Gl.DeleteShader(shader);
        throw new InvalidOperationException($"The {kind} shader failed to compile:\n{log}");
    }
}
