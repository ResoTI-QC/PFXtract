namespace PFXtract;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        // Configure DPI, polices et styles Windows avant la création de la fenêtre.
        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
    }
}
