using System.Windows;
using RetailX.Data;
using RetailX.Services;
using RetailX.ViewModels;

namespace RetailX;

public partial class App : Application
{
    protected override async void OnStartup(StartupEventArgs e)
    {
        await DatabaseInitializer.InitializeAsync();
        base.OnStartup(e);
    }

    public static AppViewModel CreateAppViewModel()
    {
        return new AppViewModel(new AuthService(), new ProductService(), new SaleService());
    }
}
