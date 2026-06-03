using System.Collections.ObjectModel;
using System.ComponentModel;
using RetailX.Models;
using RetailX.Services;

namespace RetailX.ViewModels;

public class PosViewModel : ObservableObject
{
    private static readonly List<HeldBillSnapshot> HeldBills = [];
    private readonly ProductService _productService;
    private readonly SaleService _saleService;
    private readonly int _userId;
    private string _draftBillNumber = NewDraftBillNumber();
    private string _searchText = string.Empty;
    private string _statusMessage = "Ready for barcode scan or product search.";
    private bool _isStatusError;
    private decimal _paidAmount;
    private PaymentMethod _selectedPaymentMethod = PaymentMethod.Cash;
    private CartLineViewModel? _selectedLine;
    private string _lastCompletedInvoice = "No completed sale yet";
    private string _lastReceiptSummary = "Complete a sale to see the receipt summary.";

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
        ExactCashCommand = new RelayCommand(() => SetPaidAmount(GrandTotal));
        AddCash500Command = new RelayCommand(() => SetPaidAmount(PaidAmount + 500));
        AddCash1000Command = new RelayCommand(() => SetPaidAmount(PaidAmount + 1000));
        AddCash5000Command = new RelayCommand(() => SetPaidAmount(PaidAmount + 5000));
        ClearPaidCommand = new RelayCommand(() => SetPaidAmount(0));
        HoldBillCommand = new RelayCommand(HoldCurrentBill, () => CartLines.Count > 0);
        RecallBillCommand = new RelayCommand(RecallLastHeldBill, () => HeldBills.Count > 0);
        PrintReceiptCommand = new RelayCommand(() => SetStatus($"Receipt ready for {LastCompletedInvoice}. ESC/POS printing will be wired later."));
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

    public string DraftBillNumber
    {
        get => _draftBillNumber;
        set => SetProperty(ref _draftBillNumber, value);
    }

    public decimal PaidAmount
    {
        get => _paidAmount;
        set
        {
            if (SetProperty(ref _paidAmount, Math.Max(0, value)))
            {
                OnPropertyChanged(nameof(Balance));
                OnPropertyChanged(nameof(AmountDue));
                OnPropertyChanged(nameof(ChangeDue));
                OnPropertyChanged(nameof(PaymentStatus));
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
    public decimal AmountDue => Math.Max(0, GrandTotal - PaidAmount);
    public decimal ChangeDue => Math.Max(0, PaidAmount - GrandTotal);
    public int ItemCount => CartLines.Count;
    public decimal TotalQuantity => CartLines.Sum(x => x.Quantity);
    public int HeldBillCount => HeldBills.Count;
    public string PaymentStatus => GrandTotal <= 0
        ? "No active bill"
        : PaidAmount >= GrandTotal
            ? "Ready to complete"
            : $"Due LKR {AmountDue:N2}";

    public string LastCompletedInvoice
    {
        get => _lastCompletedInvoice;
        set => SetProperty(ref _lastCompletedInvoice, value);
    }

    public string LastReceiptSummary
    {
        get => _lastReceiptSummary;
        set => SetProperty(ref _lastReceiptSummary, value);
    }

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
    public RelayCommand ExactCashCommand { get; }
    public RelayCommand AddCash500Command { get; }
    public RelayCommand AddCash1000Command { get; }
    public RelayCommand AddCash5000Command { get; }
    public RelayCommand ClearPaidCommand { get; }
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

            LastCompletedInvoice = invoice;
            LastReceiptSummary = $"{invoice} | {ItemCount} items / {TotalQuantity:N0} qty | Total LKR {GrandTotal:N2} | Paid LKR {PaidAmount:N2} | Change LKR {ChangeDue:N2}";
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
        DraftBillNumber = NewDraftBillNumber();
        RefreshTotals();
        SetStatus("Bill cleared. Ready for next customer.");
    }

    private void SelectPayment(PaymentMethod method)
    {
        SelectedPaymentMethod = method;
        if (method is PaymentMethod.Card or PaymentMethod.QR)
        {
            SetPaidAmount(GrandTotal);
        }

        SetStatus($"{method} payment selected.");
    }

    private void SetPaidAmount(decimal amount)
    {
        PaidAmount = amount;
        SetStatus(PaidAmount >= GrandTotal
            ? $"Payment covered. Change LKR {ChangeDue:N2}."
            : $"Payment captured. Amount due LKR {AmountDue:N2}.",
            GrandTotal > 0 && PaidAmount < GrandTotal);
    }

    private void HoldCurrentBill()
    {
        var snapshot = new HeldBillSnapshot(
            $"HOLD-{DateTime.Now:HHmmss}",
            DraftBillNumber,
            SelectedPaymentMethod,
            PaidAmount,
            CartLines.Select(CloneLine).ToList());

        HeldBills.Add(snapshot);
        CancelBill();
        OnPropertyChanged(nameof(HeldBillCount));
        RecallBillCommand.RaiseCanExecuteChanged();
        SetStatus($"{snapshot.HoldNumber} held. Ready for next customer.");
    }

    private void RecallLastHeldBill()
    {
        if (HeldBills.Count == 0)
        {
            SetStatus("No held bills available.", true);
            return;
        }

        CancelBill();
        var snapshot = HeldBills[^1];
        HeldBills.RemoveAt(HeldBills.Count - 1);

        DraftBillNumber = snapshot.DraftBillNumber;
        SelectedPaymentMethod = snapshot.PaymentMethod;
        PaidAmount = snapshot.PaidAmount;

        foreach (var line in snapshot.Lines.Select(CloneLine))
        {
            line.PropertyChanged += CartLineChanged;
            CartLines.Add(line);
        }

        SelectedLine = CartLines.LastOrDefault();
        RefreshTotals();
        OnPropertyChanged(nameof(HeldBillCount));
        RecallBillCommand.RaiseCanExecuteChanged();
        SetStatus($"{snapshot.HoldNumber} recalled.");
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
        OnPropertyChanged(nameof(AmountDue));
        OnPropertyChanged(nameof(ChangeDue));
        OnPropertyChanged(nameof(ItemCount));
        OnPropertyChanged(nameof(TotalQuantity));
        OnPropertyChanged(nameof(PaymentStatus));
        CompleteSaleCommand.RaiseCanExecuteChanged();
        CancelBillCommand.RaiseCanExecuteChanged();
        HoldBillCommand.RaiseCanExecuteChanged();
        RecallBillCommand.RaiseCanExecuteChanged();
    }

    private static string NewDraftBillNumber() => $"DRAFT-{DateTime.Now:yyyyMMdd-HHmmss}";

    private static CartLineViewModel CloneLine(CartLineViewModel line)
    {
        return new CartLineViewModel(
            line.ProductId,
            line.ItemCode,
            line.ProductName,
            line.UnitPrice,
            line.AvailableStock,
            line.Quantity,
            line.Discount);
    }

    private sealed record HeldBillSnapshot(
        string HoldNumber,
        string DraftBillNumber,
        PaymentMethod PaymentMethod,
        decimal PaidAmount,
        List<CartLineViewModel> Lines);
}
