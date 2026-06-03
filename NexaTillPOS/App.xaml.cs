using System.Windows;
using NexaTillPOS.Data;
using NexaTillPOS.Services;
using NexaTillPOS.ViewModels;

namespace NexaTillPOS;

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
