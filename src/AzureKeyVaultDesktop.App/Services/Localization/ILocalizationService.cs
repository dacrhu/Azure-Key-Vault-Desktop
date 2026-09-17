using System.ComponentModel;

namespace AzureKeyVaultDesktop.App.Services.Localization;

public interface ILocalizationService : INotifyPropertyChanged
{
    string CurrentLanguage { get; set; }

    IReadOnlyList<(string Code, string DisplayName)> AvailableLanguages { get; }

    /// <summary>Looks up a translated string by key, falling back to English and then to the
    /// key itself if not found — so a missing translation never crashes the UI.</summary>
    string this[string key] { get; }

    /// <summary>Looks up a translated format string and applies <see cref="string.Format"/>.</summary>
    string Translate(string key, params object[] args);
}
