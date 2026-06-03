using System.ComponentModel.DataAnnotations;

namespace NexaTillPOS.Models;

public class Product
{
    public int Id { get; set; }

    [MaxLength(40)]
    public string Barcode { get; set; } = string.Empty;

    [MaxLength(40)]
    public string SKU { get; set; } = string.Empty;

    [MaxLength(140)]
    public string Name { get; set; } = string.Empty;

    public int CategoryId { get; set; }

    public Category? Category { get; set; }

    [MaxLength(80)]
    public string Brand { get; set; } = string.Empty;

    public decimal CostPrice { get; set; }

    public decimal SellingPrice { get; set; }

    public decimal StockQuantity { get; set; }

    public decimal ReorderLevel { get; set; }

    public DateTime? ExpiryDate { get; set; }

    public bool IsActive { get; set; } = true;
}
