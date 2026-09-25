using System.Reflection;

namespace AzureKeyVaultDesktop.App.Services;

internal static class AppVersion
{
    public static string Current { get; } = Resolve();

    private static string Resolve()
    {
        var assembly = Assembly.GetExecutingAssembly();
        return assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? assembly.GetName().Version?.ToString()
            ?? "dev";
    }
}
