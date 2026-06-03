namespace RetailX.Models;

public class AppSetting
{
    public int Id { get; set; }

    public string StoreName { get; set; } = "Retail-X";

    public string BranchName { get; set; } = "Colombo Main Branch";

    public string Currency { get; set; } = "LKR";

    public decimal TaxPercentage { get; set; }

    public string ReceiptHeader { get; set; } = "Thank you for shopping with us";

    public string ReceiptFooter { get; set; } = "© 2026 Adheesha Sooriyaarachchi. CodaWix™";

    public string PrinterName { get; set; } = string.Empty;
}
