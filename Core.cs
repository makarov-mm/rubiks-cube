namespace RubiksCube;

internal static class Core
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new RubiksForm());
    }
}