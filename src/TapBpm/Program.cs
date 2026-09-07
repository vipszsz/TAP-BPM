namespace TapBpm;

internal static class Program
{
    /// <summary>
    /// Only one copy may run at a time: a second one would fight the first for the global
    /// hotkey and lose, leaving the user with a window that silently does nothing.
    /// </summary>
    private const string SingleInstanceMutex = @"Local\Vipz.TapBPM.SingleInstance";

    [STAThread]
    private static void Main()
    {
        using var mutex = new Mutex(initiallyOwned: true, SingleInstanceMutex, out bool isFirstInstance);
        if (!isFirstInstance)
            return;

        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
    }
}
