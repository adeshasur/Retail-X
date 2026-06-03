using System.Collections.ObjectModel;
using System.ComponentModel;
using RetailX.Models;
using RetailX.Services;

namespace RetailX.ViewModels;

public class PosViewModel : ObservableObject
{
    private readonly ProductService _productService;
    private readonly SaleService _saleService;
    private readonly int _userId;
    private string _searchText = string.Empty;
    private string _statusMessage = "Ready for barcode scan or product search.";
    private bool _isStatusError;
    private decimal _paidAmount;
    private PaymentMethod _selectedPaymentMethod = PaymentMethod.Cash;
    private CartLineViewModel? _selectedLine;

    public PosViewModel(ProductService productService, SaleService saleService, int userId)
    {
        _productService = productService;
        _saleService = saleService;
        _userId = userId;

        SearchResults = [];
        CartLines = [];
        SearchCommand = new AsyncRelayCommand(SearchAndAddAsync);
        CompleteSaleCommand = new AsyncRelayCommand(CompleteSaleAsync, () => CartLines.Count > 0);
        CancelBillCommand = new RelayCommand(CancelBill, () => CartLines.Count > 0);
        SetCashCommand = new RelayCommand(() => SelectPayment(PaymentMethod.Cash));
        SetCardCommand = new RelayCommand(() => SelectPayment(PaymentMethod.Card));
        SetQrCommand = new RelayCommand(() => SelectPayment(PaymentMethod.QR));
        SetSplitCommand = new RelayCommand(() => SelectPayment(PaymentMethod.Split));
        AddSelectedProductCommand = new RelayCommand<Product>(AddProduct);
        RemoveLineCommand = new RelayCommand<CartLineViewModel>(RemoveLine);
        VoidSelectedLineCommand = new RelayCommand(VoidSelectedLine);
        IncreaseQuantityCommand = new RelayCommand<CartLineViewModel>(IncreaseQuantity);
        DecreaseQuantityCommand = new RelayCommand<CartLineViewModel>(DecreaseQuantity);
        HoldBillCommand = new RelayCommand(() => SetStatus("Hold bill is planned for the next milestone."));
        RecallBillCommand = new RelayCommand(() => SetStatus("Recall bill is planned for the next milestone."));
        PrintReceiptCommand = new RelayCommand(() => SetStatus("Receipt printing via ESC/POS is planned for later."));
    }

    public ObservableCollection<Product> SearchResults { get; }
    public ObservableCollection<CartLineViewModel> CartLines { get; }

    public string SearchText
    {
        get => _searchText;
        set => SetProperty(ref _searchText, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public bool IsStatusError
    {
        get => _isStatusError;
        set => SetProperty(ref _isStatusError, value);
    }

    public decimal PaidAmount
    {
        get => _paidAmount;
        set
        {
            if (SetProperty(ref _paidAmount, Math.Max(0, value)))
            {
                OnPropertyChanged(nameof(Balance));
            }
        }
    }

    public PaymentMethod SelectedPaymentMethod
    {
        get => _selectedPaymentMethod;
        set => SetProperty(ref _selectedPaymentMethod, value);
    }

    public CartLineViewModel? SelectedLine
    {
        get => _selectedLine;
        set => SetProperty(ref _selectedLine, value);
    }

    public decimal Subtotal => CartLines.Sum(x => x.UnitPrice * x.Quantity);
    public decimal Discount => CartLines.Sum(x => x.Discount);
    public decimal Tax => 0;
    public decimal GrandTotal => Subtotal - Discount + Tax;
    public decimal Balance => Math.Max(0, PaidAmount - GrandTotal);

    public AsyncRelayCommand SearchCommand { get; }
    public AsyncRelayCommand CompleteSaleCommand { get; }
    public RelayCommand CancelBillCommand { get; }
    public RelayCommand SetCashCommand { get; }
    public RelayCommand SetCardCommand { get; }
    public RelayCommand SetQrCommand { get; }
    public RelayCommand SetSplitCommand { get; }
    public RelayCommand<Product> AddSelectedProductCommand { get; }
    public RelayCommand<CartLineViewModel> RemoveLineCommand { get; }
    public RelayCommand VoidSelectedLineCommand { get; }
    public RelayCommand<CartLineViewModel> IncreaseQuantityCommand { get; }
    public RelayCommand<CartLineViewModel> DecreaseQuantityCommand { get; }
    public RelayCommand HoldBillCommand { get; }
    public RelayCommand RecallBillCommand { get; }
    public RelayCommand PrintReceiptCommand { get; }

    public async Task SearchAndAddAsync()
    {
        if (string.IsNullOrWhiteSpace(SearchText))
        {
            SetStatus("Scan barcode or enter item name/SKU.", true);
            return;
        }

        var product = await _productService.FindSaleProductAsync(SearchText);
        if (product is not null)
        {
            AddProduct(product);
            SearchText = string.Empty;
            SearchResults.Clear();
            return;
        }

        var results = await _productService.SearchAsync(SearchText);
        SearchResults.Clear();
        foreach (var result in results)
        {
            SearchResults.Add(result);
        }

        SetStatus(results.Count == 0 ? "No matching product found." : "Select a product from the search results.", results.Count == 0);
    }

    public void AddProduct(Product product)
    {
        var existing = CartLines.FirstOrDefault(x => x.ProductId == product.Id);
        if (existing is not null)
        {
            if (existing.Quantity + 1 > existing.AvailableStock)
            {
                SetStatus($"Stock not available: only {existing.AvailableStock:N0} available for {existing.ProductName}.", true);
                return;
            }

            existing.Quantity += 1;
            SelectedLine = existing;
            SetStatus($"{existing.ProductName} quantity updated to {existing.Quantity:N0}.");
        }
        else
        {
            if (product.StockQuantity <= 0)
            {
                SetStatus($"Stock not available: {product.Name} is out of stock.", true);
                return;
            }

            var line = new CartLineViewModel(product);
            line.PropertyChanged += CartLineChanged;
            CartLines.Add(line);
            SelectedLine = line;
            SetStatus($"{product.Name} added.");
        }

        if (PaidAmount < GrandTotal)
        {
            PaidAmount = GrandTotal;
        }

        RefreshTotals();
    }

    public void RemoveLine(CartLineViewModel line)
    {
        line.PropertyChanged -= CartLineChanged;
        CartLines.Remove(line);
        if (SelectedLine == line)
        {
            SelectedLine = CartLines.LastOrDefault();
        }

        RefreshTotals();
        SetStatus($"{line.ProductName} removed.");
    }

    private void VoidSelectedLine()
    {
        if (SelectedLine is null)
        {
            SetStatus("Select an item to void.", true);
            return;
        }

        var line = SelectedLine;
        SelectedLine = null;
        RemoveLine(line);
    }

    private async Task CompleteSaleAsync()
    {
        try
        {
            var invoice = await _saleService.CompleteSaleAsync(
                _userId,
                CartLines.ToList(),
                Subtotal,
                Discount,
                Tax,
                GrandTotal,
                SelectedPaymentMethod,
                PaidAmount);

            CancelBill();
            SetStatus($"Sale completed. Invoice {invoice} saved.");
        }
        catch (Exception ex)
        {
            SetStatus(ex.Message, true);
        }
    }

    private void CancelBill()
    {
        foreach (var line in CartLines)
        {
            line.PropertyChanged -= CartLineChanged;
        }

        CartLines.Clear();
        SearchResults.Clear();
        SearchText = string.Empty;
        PaidAmount = 0;
        SelectedLine = null;
        RefreshTotals();
        SetStatus("Bill cleared. Ready for next customer.");
    }

    private void SelectPayment(PaymentMethod method)
    {
        SelectedPaymentMethod = method;
        PaidAmount = GrandTotal;
        SetStatus($"{method} payment selected.");
    }

    private void IncreaseQuantity(CartLineViewModel line)
    {
        SelectedLine = line;
        if (line.Quantity + 1 > line.AvailableStock)
        {
            SetStatus($"Stock not available: only {line.AvailableStock:N0} available for {line.ProductName}.", true);
            return;
        }

        line.Quantity += 1;
        SetStatus($"{line.ProductName} quantity updated to {line.Quantity:N0}.");
    }

    private void DecreaseQuantity(CartLineViewModel line)
    {
        SelectedLine = line;
        if (line.Quantity <= 1)
        {
            RemoveLine(line);
            return;
        }

        line.Quantity -= 1;
        SetStatus($"{line.ProductName} quantity updated to {line.Quantity:N0}.");
    }

    private void CartLineChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(CartLineViewModel.Quantity) or nameof(CartLineViewModel.Discount) or nameof(CartLineViewModel.LineTotal))
        {
            if (sender is CartLineViewModel line && line.Quantity >= line.AvailableStock)
            {
                SetStatus($"Stock limit reached for {line.ProductName}: {line.AvailableStock:N0} available.", true);
            }

            RefreshTotals();
        }
    }

    private void SetStatus(string message, bool isError = false)
    {
        StatusMessage = message;
        IsStatusError = isError;
    }

    private void RefreshTotals()
    {
        OnPropertyChanged(nameof(Subtotal));
        OnPropertyChanged(nameof(Discount));
        OnPropertyChanged(nameof(Tax));
        OnPropertyChanged(nameof(GrandTotal));
        OnPropertyChanged(nameof(Balance));
        CompleteSaleCommand.RaiseCanExecuteChanged();
        CancelBillCommand.RaiseCanExecuteChanged();
    }
}
