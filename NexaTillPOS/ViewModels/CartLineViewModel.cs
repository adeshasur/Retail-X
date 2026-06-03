using NexaTillPOS.Models;

namespace NexaTillPOS.ViewModels;

public class CartLineViewModel : ObservableObject
{
    private decimal _quantity = 1;
    private decimal _discount;

    public CartLineViewModel(Product product)
    {
        ProductId = product.Id;
        ItemCode = string.IsNullOrWhiteSpace(product.SKU) ? product.Barcode : product.SKU;
        ProductName = product.Name;
        UnitPrice = product.SellingPrice;
        AvailableStock = product.StockQuantity;
    }

    public int ProductId { get; }
    public string ItemCode { get; }
    public string ProductName { get; }
    public decimal UnitPrice { get; }
    public decimal AvailableStock { get; }

    public decimal Quantity
    {
        get => _quantity;
        set
        {
            if (SetProperty(ref _quantity, Math.Max(1, value)))
            {
                OnPropertyChanged(nameof(LineTotal));
            }
        }
    }

    public decimal Discount
    {
        get => _discount;
        set
        {
            if (SetProperty(ref _discount, Math.Max(0, value)))
            {
                OnPropertyChanged(nameof(LineTotal));
            }
        }
    }

    public decimal LineTotal => Math.Max(0, UnitPrice * Quantity - Discount);
}
