namespace NexaTillPOS.Models;

public class AppSetting
{
    public int Id { get; set; }

    public string StoreName { get; set; } = "NexaTill POS by CodaWix";

    public string BranchName { get; set; } = "Colombo Main Branch";

    public string Currency { get; set; } = "LKR";

    public decimal TaxPercentage { get; set; }

    public string ReceiptHeader { get; set; } = "Thank you for shopping with us";

    public string ReceiptFooter { get; set; } = "Powered by CodaWix";

    public string PrinterName { get; set; } = string.Empty;
}
