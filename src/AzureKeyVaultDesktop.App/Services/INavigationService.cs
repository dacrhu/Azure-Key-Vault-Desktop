using AzureKeyVaultDesktop.App.ViewModels;

namespace AzureKeyVaultDesktop.App.Services;

public interface INavigationService
{
    event EventHandler<ViewModelBase>? Navigated;

    void NavigateTo<TViewModel>() where TViewModel : ViewModelBase;

    /// <summary>Resolves TViewModel from DI, applies <paramref name="configure"/> (e.g. to pass
    /// along the selected vault) before it's displayed, then navigates to it.</summary>
    void NavigateTo<TViewModel>(Action<TViewModel> configure) where TViewModel : ViewModelBase;
}
