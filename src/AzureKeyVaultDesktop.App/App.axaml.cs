using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using AzureKeyVaultDesktop.App.Services;
using AzureKeyVaultDesktop.App.Services.Localization;
using AzureKeyVaultDesktop.App.ViewModels;
using AzureKeyVaultDesktop.App.Views;
using AzureKeyVaultDesktop.Core.Services.Auth;
using AzureKeyVaultDesktop.Core.Services.Clipboard;
using AzureKeyVaultDesktop.Core.Services.KeyVault;
using AzureKeyVaultDesktop.Core.Services.Settings;
using AzureKeyVaultDesktop.Core.Services.Updates;
using Microsoft.Extensions.DependencyInjection;

namespace AzureKeyVaultDesktop.App;

public partial class App : Application
{
    public IServiceProvider Services { get; private set; } = null!;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var services = new ServiceCollection();
        ConfigureServices(services);
        Services = services.BuildServiceProvider();

        // Avalonia installs its SynchronizationContext before this runs, so a plain
        // `.GetAwaiter().GetResult()` here would deadlock: LoadAsync()'s continuations try to
        // post back to that context, but the UI thread that would pump them is the one we're
        // blocking. Task.Run escapes to the thread pool so the awaits inside LoadAsync have no
        // UI context to marshal back to, and only the outer wait blocks this thread.
        Task.Run(() => Services.GetRequiredService<ISettingsService>().LoadAsync()).GetAwaiter().GetResult();

        var savedLanguage = Services.GetRequiredService<ISettingsService>().Current.Language;
        Services.GetRequiredService<ILocalizationService>().CurrentLanguage = savedLanguage;

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var mainWindow = new MainWindow
            {
                DataContext = Services.GetRequiredService<MainWindowViewModel>(),
            };
            desktop.MainWindow = mainWindow;
            desktop.ShutdownRequested += (_, _) =>
                Services.GetRequiredService<IClipboardService>().ClearPendingNow();
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static void ConfigureServices(ServiceCollection services)
    {
        services.AddSingleton<ISettingsService, JsonSettingsService>();
        services.AddSingleton<IAuthService, AzureIdentityAuthService>();
        services.AddSingleton<IClipboardTextAccessor, AvaloniaClipboardTextAccessor>();
        services.AddSingleton<IClipboardService, ClipboardAutoClearService>();
        services.AddSingleton<INavigationService, NavigationService>();
        services.AddSingleton<ILocalizationService>(_ => LocalizationService.Instance);

        // These take IAuthService itself, not a snapshot of .Credential — they re-read it on
        // every call and rebuild their internal clients/caches when it changes, so signing out
        // and back in as a different user can't keep serving the previous identity's data.
        services.AddSingleton<IVaultListCacheStore, JsonVaultListCacheStore>();
        services.AddSingleton<IKeyVaultManagementService, KeyVaultManagementService>();
        services.AddSingleton<IKeyVaultSecretsService, KeyVaultSecretsService>();

        // Singleton so the (memoized) GitHub API check only ever runs once per app run.
        services.AddSingleton<IUpdateCheckService, GitHubReleaseUpdateCheckService>();

        services.AddSingleton<MainWindowViewModel>();

        // Transient: each navigation gets a fresh instance so its constructor-driven initial
        // load (or explicit Initialize(...) call) reruns every time the page is shown again.
        services.AddTransient<LoginViewModel>();
        services.AddTransient<SetupGuideViewModel>();
        services.AddTransient<VaultListViewModel>();
        services.AddTransient<SecretListViewModel>();
        services.AddTransient<SecretDetailViewModel>();
        services.AddTransient<NewSecretViewModel>();
        services.AddTransient<SettingsViewModel>();
    }
}
