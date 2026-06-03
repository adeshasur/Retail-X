using System.Collections.ObjectModel;
using System.ComponentModel;
using Microsoft.EntityFrameworkCore;
using RetailX.Data;
using RetailX.Models;
using RetailX.Services;

namespace RetailX.ViewModels;

public class PosViewModel : ObservableObject
{
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
    private string _lastScannedItemName = "No item scanned";
    private string _lastScannedItemCode = "-";
    private decimal _lastScannedItemPrice;
    private decimal _lastScannedItemStock;
    private bool _isShiftOpen;
    private DateTime? _shiftOpenedAt;
    private decimal _openingCash = 5000;
    private decimal _closingCash;
    private decimal _shiftSalesTotal;
    private decimal _shiftCashTotal;
    private decimal _shiftCardTotal;
    private decimal _shiftQrTotal;
    private int _shiftBillCount;
    private int _heldBillCount;
    private bool _isQuantityDialogOpen;
    private bool _isDiscountDialogOpen;
    private decimal _quantityInput = 1;
    private decimal _discountInput;

    public PosViewModel(ProductService productService, SaleService saleService, int userId)
    {
        _productService = productService;
        _saleService = saleService;
        _userId = userId;

        SearchResults = [];
        CartLines = [];
        SearchCommand = new AsyncRelayCommand(SearchAndAddAsync);
        CompleteSaleCommand = new AsyncRelayCommand(CompleteSaleAsync, () => CartLines.Count > 0 && IsShiftOpen);
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
        RecallBillCommand = new RelayCommand(RecallLastHeldBill, () => HeldBillCount > 0);
        PrintReceiptCommand = new RelayCommand(() => SetStatus($"Receipt ready for {LastCompletedInvoice}. ESC/POS printing will be wired later."));
        OpenShiftCommand = new RelayCommand(OpenShift, () => !IsShiftOpen);
        CloseShiftCommand = new RelayCommand(CloseShift, () => IsShiftOpen && CartLines.Count == 0);
        OpenQuantityDialogCommand = new RelayCommand(OpenQuantityDialog, () => SelectedLine is not null);
        ApplyQuantityCommand = new RelayCommand(ApplyQuantityDialog);
        CloseQuantityDialogCommand = new RelayCommand(() => IsQuantityDialogOpen = false);
        OpenDiscountDialogCommand = new RelayCommand(OpenDiscountDialog, () => SelectedLine is not null);
        ApplyDiscountCommand = new RelayCommand(ApplyDiscountDialog);
        CloseDiscountDialogCommand = new RelayCommand(() => IsDiscountDialogOpen = false);

        _ = LoadHeldBillCountAsync();
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
        set
        {
            if (SetProperty(ref _selectedLine, value))
            {
                OpenQuantityDialogCommand.RaiseCanExecuteChanged();
                OpenDiscountDialogCommand.RaiseCanExecuteChanged();
            }
        }
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
    public int HeldBillCount
    {
        get => _heldBillCount;
        set
        {
            if (SetProperty(ref _heldBillCount, value))
            {
                RecallBillCommand.RaiseCanExecuteChanged();
            }
        }
    }
    public string PaymentStatus => GrandTotal <= 0
        ? "No active bill"
        : PaidAmount >= GrandTotal
            ? "Ready to complete"
            : $"Due LKR {AmountDue:N2}";
    public string ShiftStatus => IsShiftOpen
        ? $"Open since {ShiftOpenedAt:HH:mm} | Bills {ShiftBillCount} | Sales LKR {ShiftSalesTotal:N2}"
        : "Shift closed";
    public decimal ExpectedCash => OpeningCash + ShiftCashTotal;
    public decimal CashVariance => ClosingCash - ExpectedCash;
    public string ReceiptPreviewText => BuildReceiptPreview();

    public bool IsQuantityDialogOpen
    {
        get => _isQuantityDialogOpen;
        set => SetProperty(ref _isQuantityDialogOpen, value);
    }

    public bool IsDiscountDialogOpen
    {
        get => _isDiscountDialogOpen;
        set => SetProperty(ref _isDiscountDialogOpen, value);
    }

    public decimal QuantityInput
    {
        get => _quantityInput;
        set => SetProperty(ref _quantityInput, Math.Max(1, value));
    }

    public decimal DiscountInput
    {
        get => _discountInput;
        set => SetProperty(ref _discountInput, Math.Max(0, value));
    }

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

    public string LastScannedItemName
    {
        get => _lastScannedItemName;
        set => SetProperty(ref _lastScannedItemName, value);
    }

    public string LastScannedItemCode
    {
        get => _lastScannedItemCode;
        set => SetProperty(ref _lastScannedItemCode, value);
    }

    public decimal LastScannedItemPrice
    {
        get => _lastScannedItemPrice;
        set => SetProperty(ref _lastScannedItemPrice, value);
    }

    public decimal LastScannedItemStock
    {
        get => _lastScannedItemStock;
        set => SetProperty(ref _lastScannedItemStock, value);
    }

    public bool IsShiftOpen
    {
        get => _isShiftOpen;
        set
        {
            if (SetProperty(ref _isShiftOpen, value))
            {
                OnPropertyChanged(nameof(ShiftStatus));
                OpenShiftCommand.RaiseCanExecuteChanged();
                CloseShiftCommand.RaiseCanExecuteChanged();
                CompleteSaleCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public DateTime? ShiftOpenedAt
    {
        get => _shiftOpenedAt;
        set
        {
            if (SetProperty(ref _shiftOpenedAt, value))
            {
                OnPropertyChanged(nameof(ShiftStatus));
            }
        }
    }

    public decimal OpeningCash
    {
        get => _openingCash;
        set
        {
            if (SetProperty(ref _openingCash, Math.Max(0, value)))
            {
                OnPropertyChanged(nameof(ExpectedCash));
                OnPropertyChanged(nameof(CashVariance));
            }
        }
    }

    public decimal ClosingCash
    {
        get => _closingCash;
        set
        {
            if (SetProperty(ref _closingCash, Math.Max(0, value)))
            {
                OnPropertyChanged(nameof(CashVariance));
            }
        }
    }

    public decimal ShiftSalesTotal
    {
        get => _shiftSalesTotal;
        set
        {
            if (SetProperty(ref _shiftSalesTotal, value))
            {
                OnPropertyChanged(nameof(ShiftStatus));
            }
        }
    }

    public decimal ShiftCashTotal
    {
        get => _shiftCashTotal;
        set
        {
            if (SetProperty(ref _shiftCashTotal, value))
            {
                OnPropertyChanged(nameof(ExpectedCash));
                OnPropertyChanged(nameof(CashVariance));
            }
        }
    }

    public decimal ShiftCardTotal
    {
        get => _shiftCardTotal;
        set => SetProperty(ref _shiftCardTotal, value);
    }

    public decimal ShiftQrTotal
    {
        get => _shiftQrTotal;
        set => SetProperty(ref _shiftQrTotal, value);
    }

    public int ShiftBillCount
    {
        get => _shiftBillCount;
        set
        {
            if (SetProperty(ref _shiftBillCount, value))
            {
                OnPropertyChanged(nameof(ShiftStatus));
            }
        }
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
    public RelayCommand OpenShiftCommand { get; }
    public RelayCommand CloseShiftCommand { get; }
    public RelayCommand OpenQuantityDialogCommand { get; }
    public RelayCommand ApplyQuantityCommand { get; }
    public RelayCommand CloseQuantityDialogCommand { get; }
    public RelayCommand OpenDiscountDialogCommand { get; }
    public RelayCommand ApplyDiscountCommand { get; }
    public RelayCommand CloseDiscountDialogCommand { get; }

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
        LastScannedItemName = product.Name;
        LastScannedItemCode = string.IsNullOrWhiteSpace(product.SKU) ? product.Barcode : product.SKU;
        LastScannedItemPrice = product.SellingPrice;
        LastScannedItemStock = product.StockQuantity;

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
            if (!IsShiftOpen)
            {
                SetStatus("Open cashier shift before completing sales.", true);
                return;
            }

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
            ApplyShiftSaleTotals(GrandTotal, SelectedPaymentMethod, PaidAmount);
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
        LastScannedItemName = "No item scanned";
        LastScannedItemCode = "-";
        LastScannedItemPrice = 0;
        LastScannedItemStock = 0;
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

    private void OpenShift()
    {
        ShiftOpenedAt = DateTime.Now;
        IsShiftOpen = true;
        ClosingCash = 0;
        ShiftSalesTotal = 0;
        ShiftCashTotal = 0;
        ShiftCardTotal = 0;
        ShiftQrTotal = 0;
        ShiftBillCount = 0;
        SetStatus($"Shift opened with LKR {OpeningCash:N2} opening cash.");
    }

    private void CloseShift()
    {
        ClosingCash = ClosingCash <= 0 ? ExpectedCash : ClosingCash;
        SetStatus($"Shift closed. Expected cash LKR {ExpectedCash:N2}. Actual LKR {ClosingCash:N2}. Variance LKR {CashVariance:N2}.", CashVariance != 0);
        IsShiftOpen = false;
        ShiftOpenedAt = null;
    }

    private void ApplyShiftSaleTotals(decimal saleTotal, PaymentMethod method, decimal paidAmount)
    {
        ShiftSalesTotal += saleTotal;
        ShiftBillCount += 1;

        switch (method)
        {
            case PaymentMethod.Cash:
                ShiftCashTotal += saleTotal;
                break;
            case PaymentMethod.Card:
                ShiftCardTotal += saleTotal;
                break;
            case PaymentMethod.QR:
                ShiftQrTotal += saleTotal;
                break;
            case PaymentMethod.Split:
                ShiftCashTotal += Math.Min(paidAmount, saleTotal);
                break;
        }
    }

    private void OpenQuantityDialog()
    {
        if (SelectedLine is null)
        {
            SetStatus("Select an item before changing quantity.", true);
            return;
        }

        QuantityInput = SelectedLine.Quantity;
        IsDiscountDialogOpen = false;
        IsQuantityDialogOpen = true;
        SetStatus($"Editing quantity for {SelectedLine.ProductName}.");
    }

    private void ApplyQuantityDialog()
    {
        if (SelectedLine is null)
        {
            IsQuantityDialogOpen = false;
            SetStatus("No item selected for quantity change.", true);
            return;
        }

        if (QuantityInput > SelectedLine.AvailableStock)
        {
            SetStatus($"Stock not available: only {SelectedLine.AvailableStock:N0} available for {SelectedLine.ProductName}.", true);
            return;
        }

        SelectedLine.Quantity = QuantityInput;
        IsQuantityDialogOpen = false;
        RefreshTotals();
        SetStatus($"{SelectedLine.ProductName} quantity set to {SelectedLine.Quantity:N0}.");
    }

    private void OpenDiscountDialog()
    {
        if (SelectedLine is null)
        {
            SetStatus("Select an item before applying discount.", true);
            return;
        }

        DiscountInput = SelectedLine.Discount;
        IsQuantityDialogOpen = false;
        IsDiscountDialogOpen = true;
        SetStatus($"Editing discount for {SelectedLine.ProductName}.");
    }

    private void ApplyDiscountDialog()
    {
        if (SelectedLine is null)
        {
            IsDiscountDialogOpen = false;
            SetStatus("No item selected for discount.", true);
            return;
        }

        var lineGross = SelectedLine.UnitPrice * SelectedLine.Quantity;
        if (DiscountInput > lineGross)
        {
            SetStatus("Discount cannot exceed item line total.", true);
            return;
        }

        var managerApprovalThreshold = lineGross * 0.20m;
        if (DiscountInput > managerApprovalThreshold)
        {
            SetStatus("Manager approval required for discounts above 20%. Placeholder approval accepted for development.", true);
        }
        else
        {
            SetStatus($"{SelectedLine.ProductName} discount applied.");
        }

        SelectedLine.Discount = DiscountInput;
        IsDiscountDialogOpen = false;
        RefreshTotals();
    }

    private void SetPaidAmount(decimal amount)
    {
        PaidAmount = amount;
        SetStatus(PaidAmount >= GrandTotal
            ? $"Payment covered. Change LKR {ChangeDue:N2}."
            : $"Payment captured. Amount due LKR {AmountDue:N2}.",
            GrandTotal > 0 && PaidAmount < GrandTotal);
    }

    private async void HoldCurrentBill()
    {
        try
        {
            await using var db = new PosDbContext();
            var holdNumber = $"HOLD-{DateTime.Now:yyyyMMdd-HHmmss}";

            db.HeldBills.Add(new HeldBill
            {
                HoldNumber = holdNumber,
                UserId = _userId,
                HeldAt = DateTime.Now,
                GrandTotal = GrandTotal,
                Items = CartLines.Select(line => new HeldBillItem
                {
                    ProductId = line.ProductId,
                    Quantity = line.Quantity,
                    UnitPrice = line.UnitPrice,
                    Discount = line.Discount
                }).ToList()
            });

            await db.SaveChangesAsync();
            await LoadHeldBillCountAsync();

            CancelBill();
            SetStatus($"{holdNumber} held in database. Ready for next customer.");
        }
        catch (Exception ex)
        {
            SetStatus($"Could not hold bill: {ex.Message}", true);
        }
    }

    private async void RecallLastHeldBill()
    {
        try
        {
            await using var db = new PosDbContext();
            var heldBill = await db.HeldBills
                .Include(x => x.Items)
                .ThenInclude(x => x.Product)
                .OrderByDescending(x => x.HeldAt)
                .FirstOrDefaultAsync(x => x.UserId == _userId);

            if (heldBill is null)
            {
                await LoadHeldBillCountAsync();
                SetStatus("No held bills available.", true);
                return;
            }

            CancelBill();
            DraftBillNumber = heldBill.HoldNumber;
            PaidAmount = 0;
            SelectedPaymentMethod = PaymentMethod.Cash;

            foreach (var item in heldBill.Items)
            {
                if (item.Product is null)
                {
                    continue;
                }

                var line = new CartLineViewModel(
                    item.ProductId,
                    string.IsNullOrWhiteSpace(item.Product.SKU) ? item.Product.Barcode : item.Product.SKU,
                    item.Product.Name,
                    item.UnitPrice,
                    item.Product.StockQuantity,
                    item.Quantity,
                    item.Discount);

                line.PropertyChanged += CartLineChanged;
                CartLines.Add(line);
            }

            db.HeldBills.Remove(heldBill);
            await db.SaveChangesAsync();
            await LoadHeldBillCountAsync();

            SelectedLine = CartLines.LastOrDefault();
            RefreshTotals();
            SetStatus($"{heldBill.HoldNumber} recalled from database.");
        }
        catch (Exception ex)
        {
            SetStatus($"Could not recall bill: {ex.Message}", true);
        }
    }

    private async Task LoadHeldBillCountAsync()
    {
        try
        {
            await using var db = new PosDbContext();
            HeldBillCount = await db.HeldBills.CountAsync(x => x.UserId == _userId);
        }
        catch
        {
            HeldBillCount = 0;
        }
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
        OnPropertyChanged(nameof(ReceiptPreviewText));
        CompleteSaleCommand.RaiseCanExecuteChanged();
        CancelBillCommand.RaiseCanExecuteChanged();
        HoldBillCommand.RaiseCanExecuteChanged();
        RecallBillCommand.RaiseCanExecuteChanged();
        CloseShiftCommand.RaiseCanExecuteChanged();
    }

    private string BuildReceiptPreview()
    {
        var lines = CartLines.Count == 0
            ? "No active basket."
            : string.Join(Environment.NewLine, CartLines.Select(x => $"{x.ProductName} x{x.Quantity:N0}  {x.LineTotal:N2}"));

        return $"""
               Retail-X
               {DraftBillNumber}
               ------------------------------
               {lines}
               ------------------------------
               Subtotal      {Subtotal:N2}
               Discount      {Discount:N2}
               VAT/Tax       {Tax:N2}
               TOTAL         {GrandTotal:N2}
               Paid          {PaidAmount:N2}
               Change        {ChangeDue:N2}
               """;
    }

    private static string NewDraftBillNumber() => $"DRAFT-{DateTime.Now:yyyyMMdd-HHmmss}";
}
