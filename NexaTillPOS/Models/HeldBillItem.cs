namespace NexaTillPOS.Models;

public class HeldBillItem
{
    public int Id { get; set; }

    public int HeldBillId { get; set; }

    public HeldBill? HeldBill { get; set; }

    public int ProductId { get; set; }

    public Product? Product { get; set; }

    public decimal Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal Discount { get; set; }
}
