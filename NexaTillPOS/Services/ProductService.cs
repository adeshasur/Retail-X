using Microsoft.EntityFrameworkCore;
using NexaTillPOS.Data;
using NexaTillPOS.Models;

namespace NexaTillPOS.Services;

public class ProductService
{
    public async Task<List<Product>> SearchAsync(string query, int take = 20)
    {
        await using var db = new PosDbContext();
        query = query.Trim();

        if (string.IsNullOrWhiteSpace(query))
        {
            return await db.Products
                .Include(x => x.Category)
                .Where(x => x.IsActive)
                .OrderBy(x => x.Name)
                .Take(take)
                .AsNoTracking()
                .ToListAsync();
        }

        return await db.Products
            .Include(x => x.Category)
            .Where(x => x.IsActive &&
                (x.Barcode == query ||
                 x.SKU.Contains(query) ||
                 x.Name.Contains(query)))
            .OrderByDescending(x => x.Barcode == query)
            .ThenBy(x => x.Name)
            .Take(take)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<Product?> FindSaleProductAsync(string query)
    {
        var matches = await SearchAsync(query, 10);
        return matches.FirstOrDefault(x => x.Barcode.Equals(query.Trim(), StringComparison.OrdinalIgnoreCase) ||
                                           x.SKU.Equals(query.Trim(), StringComparison.OrdinalIgnoreCase))
            ?? (matches.Count == 1 ? matches[0] : null);
    }

    public async Task<List<Product>> GetLowStockAsync()
    {
        await using var db = new PosDbContext();
        return await db.Products
            .Include(x => x.Category)
            .Where(x => x.IsActive && x.StockQuantity <= x.ReorderLevel)
            .OrderBy(x => x.StockQuantity)
            .AsNoTracking()
            .ToListAsync();
    }
}
