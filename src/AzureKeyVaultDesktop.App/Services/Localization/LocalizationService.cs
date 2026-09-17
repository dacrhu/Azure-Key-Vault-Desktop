using System.ComponentModel;

namespace AzureKeyVaultDesktop.App.Services.Localization;

/// <summary>A single static instance backs both DI-injected use (ViewModels formatting dynamic
/// messages) and direct XAML binding (via {x:Static LocalizationService.Instance}), so both
/// paths always see the same current language with no duplicate state to keep in sync.</summary>
public class LocalizationService : ILocalizationService
{
    public static LocalizationService Instance { get; } = new();

    private const string FallbackLanguage = "en";

    private string _currentLanguage = FallbackLanguage;

    public event PropertyChangedEventHandler? PropertyChanged;

    public IReadOnlyList<(string Code, string DisplayName)> AvailableLanguages { get; } = new[]
    {
        ("en", "English"),
        ("hu", "Magyar"),
        ("de", "Deutsch"),
    };

    public string CurrentLanguage
    {
        get => _currentLanguage;
        set
        {
            if (_currentLanguage == value || !Translations.All.ContainsKey(value))
            {
                return;
            }

            _currentLanguage = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentLanguage)));
            // The special "Item[]" name tells WPF/Avalonia-style bindings that every indexer
            // binding (every {Binding [SomeKey]} in the whole app) should re-evaluate.
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
        }
    }

    public string this[string key]
    {
        get
        {
            if (Translations.All.TryGetValue(_currentLanguage, out var map) && map.TryGetValue(key, out var value))
            {
                return value;
            }

            if (Translations.All.TryGetValue(FallbackLanguage, out var fallback) && fallback.TryGetValue(key, out var fallbackValue))
            {
                return fallbackValue;
            }

            return key;
        }
    }

    public string Translate(string key, params object[] args)
    {
        var format = this[key];
        return args.Length == 0 ? format : string.Format(format, args);
    }
}
