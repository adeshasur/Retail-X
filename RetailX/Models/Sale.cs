namespace RetailX.Models;

public class Sale
{
    public int Id { get; set; }

    public string InvoiceNumber { get; set; } = string.Empty;

    public DateTime SaleDate { get; set; } = DateTime.Now;

    public int UserId { get; set; }

    public User? User { get; set; }

    public decimal Subtotal { get; set; }

    public decimal Discount { get; set; }

    public decimal Tax { get; set; }

    public decimal GrandTotal { get; set; }

    public List<SaleItem> Items { get; set; } = [];

    public List<Payment> Payments { get; set; } = [];
}
