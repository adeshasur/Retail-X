using NexaTillPOS.Models;
using NexaTillPOS.Services;

namespace NexaTillPOS.ViewModels;

public class AppViewModel : ObservableObject
{
    private readonly AuthService _authService;
    private object _currentViewModel;

    public AppViewModel(AuthService authService, ProductService productService, SaleService saleService)
    {
        _authService = authService;
        _currentViewModel = new LoginViewModel(authService, OnLoggedIn);
        ProductService = productService;
        SaleService = saleService;
    }

    public ProductService ProductService { get; }

    public SaleService SaleService { get; }

    public object CurrentViewModel
    {
        get => _currentViewModel;
        private set => SetProperty(ref _currentViewModel, value);
    }

    private void OnLoggedIn(User user)
    {
        CurrentViewModel = new ShellViewModel(_authService, ProductService, SaleService, Logout);
    }

    private void Logout()
    {
        _authService.Logout();
        CurrentViewModel = new LoginViewModel(_authService, OnLoggedIn);
    }
}
