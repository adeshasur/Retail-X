namespace RetailX.Models;

public class SaleItem
{
    public int Id { get; set; }

    public int SaleId { get; set; }

    public Sale? Sale { get; set; }

    public int ProductId { get; set; }

    public Product? Product { get; set; }

    public string ItemCode { get; set; } = string.Empty;

    public string ProductName { get; set; } = string.Empty;

    public decimal UnitPrice { get; set; }

    public decimal Quantity { get; set; }

    public decimal Discount { get; set; }

    public decimal LineTotal { get; set; }
}
