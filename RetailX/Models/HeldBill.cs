namespace RetailX.Models;

public class HeldBill
{
    public int Id { get; set; }

    public string HoldNumber { get; set; } = string.Empty;

    public int UserId { get; set; }

    public User? User { get; set; }

    public DateTime HeldAt { get; set; } = DateTime.Now;

    public decimal GrandTotal { get; set; }

    public List<HeldBillItem> Items { get; set; } = [];
}
