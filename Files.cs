namespace RubiksCube;

public static class Files
{
    public static string ShaderVertex => Path.Combine(AppContext.BaseDirectory, "Shaders", "cube.vert");
    public static string ShaderFragment => Path.Combine(AppContext.BaseDirectory, "Shaders", "cube.frag");
}