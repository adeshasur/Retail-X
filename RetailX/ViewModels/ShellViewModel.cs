using System.Windows.Threading;
using Microsoft.EntityFrameworkCore;
using RetailX.Data;
using RetailX.Models;
using RetailX.Services;

namespace RetailX.ViewModels;

public class ShellViewModel : ObservableObject
{
    private readonly AuthService _authService;
    private readonly ProductService _productService;
    private readonly SaleService _saleService;
    private object _currentModule = null!;
    private string _currentDateTime = DateTime.Now.ToString("dddd, dd MMM yyyy  HH:mm:ss");
    private string _branchName = "Colombo Main Branch";

    public ShellViewModel(AuthService authService, ProductService productService, SaleService saleService, Action logout)
    {
        _authService = authService;
        _productService = productService;
        _saleService = saleService;
        LogoutCommand = new RelayCommand(logout);
        NavigatePosCommand = new RelayCommand(() => ShowPos());
        NavigateProductsCommand = new RelayCommand(() => CurrentModule = new PlaceholderViewModel("Products", "Product add, edit, delete, search and filter tools will be implemented in the next milestone."));
        NavigateInventoryCommand = new RelayCommand(() => CurrentModule = new PlaceholderViewModel("Inventory Management", "Stock in, stock adjustments, movement history, low stock and expiry tracking placeholders."));
        NavigateReportsCommand = new RelayCommand(() => CurrentModule = new PlaceholderViewModel("Reports", "Today sales, top products, low stock and payment summaries will be implemented in the next milestone."));
        NavigateSettingsCommand = new RelayCommand(() => CurrentModule = new PlaceholderViewModel("Settings", "Store profile, branch, LKR currency, VAT percentage and ESC/POS printer setup placeholders."));

        ShowPos();
        LoadSettings();
        StartClock();
    }

    public string AppName => "Retail-X";

    public string CashierName => _authService.CurrentUser?.FullName ?? "Unknown";

    public string UserRole => _authService.CurrentUser?.Role.ToString() ?? string.Empty;

    public int CurrentUserId => _authService.CurrentUser?.Id ?? 0;

    public string BranchName
    {
        get => _branchName;
        private set => SetProperty(ref _branchName, value);
    }

    public string CurrentDateTime
    {
        get => _currentDateTime;
        private set => SetProperty(ref _currentDateTime, value);
    }

    public object CurrentModule
    {
        get => _currentModule;
        set => SetProperty(ref _currentModule, value);
    }

    public RelayCommand NavigatePosCommand { get; }
    public RelayCommand NavigateProductsCommand { get; }
    public RelayCommand NavigateInventoryCommand { get; }
    public RelayCommand NavigateReportsCommand { get; }
    public RelayCommand NavigateSettingsCommand { get; }
    public RelayCommand LogoutCommand { get; }

    private void ShowPos()
    {
        CurrentModule = new PosViewModel(_productService, _saleService, CurrentUserId);
    }

    private async void LoadSettings()
    {
        await using var db = new PosDbContext();
        var settings = await db.AppSettings.AsNoTracking().FirstOrDefaultAsync();
        BranchName = settings?.BranchName ?? BranchName;
    }

    private void StartClock()
    {
        var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        timer.Tick += (_, _) => CurrentDateTime = DateTime.Now.ToString("dddd, dd MMM yyyy  HH:mm:ss");
        timer.Start();
    }
}
