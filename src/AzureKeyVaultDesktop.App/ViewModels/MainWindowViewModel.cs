using AzureKeyVaultDesktop.App.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AzureKeyVaultDesktop.App.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    [ObservableProperty]
    private ViewModelBase _currentPage;

    public MainWindowViewModel(INavigationService navigationService, LoginViewModel loginViewModel)
    {
        navigationService.Navigated += (_, viewModel) => CurrentPage = viewModel;
        _currentPage = loginViewModel;
    }
}
