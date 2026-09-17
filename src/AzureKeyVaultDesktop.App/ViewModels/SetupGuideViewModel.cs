using AzureKeyVaultDesktop.App.Services;
using CommunityToolkit.Mvvm.Input;

namespace AzureKeyVaultDesktop.App.ViewModels;

public partial class SetupGuideViewModel : ViewModelBase
{
    private readonly INavigationService _navigationService;

    public SetupGuideViewModel(INavigationService navigationService)
    {
        _navigationService = navigationService;
    }

    [RelayCommand]
    private void Back() => _navigationService.NavigateTo<LoginViewModel>();
}
