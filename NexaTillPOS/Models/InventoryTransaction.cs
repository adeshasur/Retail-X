namespace NexaTillPOS.Models;

public class InventoryTransaction
{
    public int Id { get; set; }

    public int ProductId { get; set; }

    public Product? Product { get; set; }

    public InventoryTransactionType Type { get; set; }

    public decimal Quantity { get; set; }

    public string Reference { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
