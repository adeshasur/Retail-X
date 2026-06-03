using System.Collections.ObjectModel;
using RetailX.Models;
using RetailX.Services;

namespace RetailX.ViewModels;

public class ProductsViewModel : ObservableObject
{
    private readonly ProductService _productService;
    private string _searchText = string.Empty;
    private string _statusMessage = "Product add/edit/delete forms are planned in the next step.";

    public ProductsViewModel(ProductService productService)
    {
        _productService = productService;
        Products = [];
        SearchCommand = new AsyncRelayCommand(LoadAsync);
        _ = LoadAsync();
    }

    public ObservableCollection<Product> Products { get; }

    public string SearchText
    {
        get => _searchText;
        set => SetProperty(ref _searchText, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public AsyncRelayCommand SearchCommand { get; }

    private async Task LoadAsync()
    {
        var products = await _productService.SearchAsync(SearchText, 200);
        Products.Clear();
        foreach (var product in products)
        {
            Products.Add(product);
        }
    }
}
