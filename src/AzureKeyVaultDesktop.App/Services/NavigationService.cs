using AzureKeyVaultDesktop.App.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace AzureKeyVaultDesktop.App.Services;

public class NavigationService : INavigationService
{
    private readonly IServiceProvider _serviceProvider;

    public NavigationService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public event EventHandler<ViewModelBase>? Navigated;

    public void NavigateTo<TViewModel>() where TViewModel : ViewModelBase =>
        NavigateTo<TViewModel>(_ => { });

    public void NavigateTo<TViewModel>(Action<TViewModel> configure) where TViewModel : ViewModelBase
    {
        var viewModel = _serviceProvider.GetRequiredService<TViewModel>();
        configure(viewModel);
        Navigated?.Invoke(this, viewModel);
    }
}
