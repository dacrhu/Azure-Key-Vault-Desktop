using System.Diagnostics;
using Avalonia;
using Avalonia.Logging;

namespace AzureKeyVaultDesktop.App;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        // Route Avalonia's diagnostic trace to the console, warnings and up only — enough to
        // catch a real problem (e.g. a window failing to appear) without flooding the console
        // with per-frame layout/property noise during normal operation.
        Trace.Listeners.Add(new ConsoleTraceListener());

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace(LogEventLevel.Warning);
}
