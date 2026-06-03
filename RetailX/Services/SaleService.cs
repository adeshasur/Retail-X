using Microsoft.EntityFrameworkCore;
using RetailX.Data;
using RetailX.Models;
using RetailX.ViewModels;

namespace RetailX.Services;

public class SaleService
{
    public async Task<string> CompleteSaleAsync(
        int userId,
        IReadOnlyCollection<CartLineViewModel> cartLines,
        decimal subtotal,
        decimal discount,
        decimal tax,
        decimal grandTotal,
        PaymentMethod method,
        decimal paidAmount)
    {
        if (cartLines.Count == 0)
        {
            throw new InvalidOperationException("Add at least one item before completing the sale.");
        }

        if (paidAmount < grandTotal)
        {
            throw new InvalidOperationException("Paid amount is less than the grand total.");
        }

        await using var db = new PosDbContext();
        await using var transaction = await db.Database.BeginTransactionAsync();

        var invoiceNumber = await GenerateInvoiceNumberAsync(db);
        var productIds = cartLines.Select(x => x.ProductId).ToList();
        var products = await db.Products.Where(x => productIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id);

        foreach (var line in cartLines)
        {
            if (!products.TryGetValue(line.ProductId, out var product))
            {
                throw new InvalidOperationException($"{line.ProductName} is no longer available.");
            }

            if (product.StockQuantity < line.Quantity)
            {
                throw new InvalidOperationException($"Insufficient stock for {line.ProductName}.");
            }

            product.StockQuantity -= line.Quantity;
        }

        var sale = new Sale
        {
            InvoiceNumber = invoiceNumber,
            SaleDate = DateTime.Now,
            UserId = userId,
            Subtotal = subtotal,
            Discount = discount,
            Tax = tax,
            GrandTotal = grandTotal,
            Items = cartLines.Select(line => new SaleItem
            {
                ProductId = line.ProductId,
                ItemCode = line.ItemCode,
                ProductName = line.ProductName,
                UnitPrice = line.UnitPrice,
                Quantity = line.Quantity,
                Discount = line.Discount,
                LineTotal = line.LineTotal
            }).ToList(),
            Payments =
            [
                new Payment
                {
                    Method = method,
                    Amount = paidAmount,
                    PaidAt = DateTime.Now
                }
            ]
        };

        db.Sales.Add(sale);

        foreach (var line in cartLines)
        {
            db.InventoryTransactions.Add(new InventoryTransaction
            {
                ProductId = line.ProductId,
                Type = InventoryTransactionType.Sale,
                Quantity = -line.Quantity,
                Reference = invoiceNumber,
                CreatedAt = DateTime.Now
            });
        }

        await db.SaveChangesAsync();
        await transaction.CommitAsync();

        return invoiceNumber;
    }

    private static async Task<string> GenerateInvoiceNumberAsync(PosDbContext db)
    {
        var prefix = $"RX-{DateTime.Now:yyyyMMdd}-";
        var count = await db.Sales.CountAsync(x => x.InvoiceNumber.StartsWith(prefix)) + 1;
        return $"{prefix}{count:0000}";
    }
}
