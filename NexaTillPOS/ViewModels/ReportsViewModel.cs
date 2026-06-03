using Microsoft.EntityFrameworkCore;
using NexaTillPOS.Data;

namespace NexaTillPOS.ViewModels;

public class ReportsViewModel : ObservableObject
{
    private decimal _todaySalesTotal;
    private int _todayBillCount;
    private int _lowStockCount;
    private string _paymentSummary = "No payments yet";

    public ReportsViewModel()
    {
        _ = LoadAsync();
    }

    public decimal TodaySalesTotal
    {
        get => _todaySalesTotal;
        set => SetProperty(ref _todaySalesTotal, value);
    }

    public int TodayBillCount
    {
        get => _todayBillCount;
        set => SetProperty(ref _todayBillCount, value);
    }

    public int LowStockCount
    {
        get => _lowStockCount;
        set => SetProperty(ref _lowStockCount, value);
    }

    public string PaymentSummary
    {
        get => _paymentSummary;
        set => SetProperty(ref _paymentSummary, value);
    }

    private async Task LoadAsync()
    {
        await using var db = new PosDbContext();
        var today = DateTime.Today;
        var tomorrow = today.AddDays(1);

        TodaySalesTotal = await db.Sales
            .Where(x => x.SaleDate >= today && x.SaleDate < tomorrow)
            .SumAsync(x => (decimal?)x.GrandTotal) ?? 0;

        TodayBillCount = await db.Sales.CountAsync(x => x.SaleDate >= today && x.SaleDate < tomorrow);
        LowStockCount = await db.Products.CountAsync(x => x.IsActive && x.StockQuantity <= x.ReorderLevel);

        var payments = await db.Payments
            .Where(x => x.PaidAt >= today && x.PaidAt < tomorrow)
            .GroupBy(x => x.Method)
            .Select(x => new { Method = x.Key, Total = x.Sum(p => p.Amount) })
            .ToListAsync();

        PaymentSummary = payments.Count == 0
            ? "No payments yet"
            : string.Join("   ", payments.Select(x => $"{x.Method}: LKR {x.Total:N2}"));
    }
}
