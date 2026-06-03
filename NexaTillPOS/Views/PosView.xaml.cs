using System.Windows.Controls;
using System.Windows.Input;
using NexaTillPOS.Models;
using NexaTillPOS.ViewModels;

namespace NexaTillPOS.Views;

public partial class PosView : UserControl
{
    public PosView()
    {
        InitializeComponent();
    }

    private void OnLoaded(object sender, System.Windows.RoutedEventArgs e)
    {
        SearchBox.Focus();
        Keyboard.Focus(SearchBox);
    }

    private void OnSearchKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && DataContext is PosViewModel viewModel)
        {
            viewModel.SearchCommand.Execute(null);
            SearchBox.Focus();
            e.Handled = true;
        }
    }

    private void OnSearchResultDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is ListBox { SelectedItem: Product product } && DataContext is PosViewModel viewModel)
        {
            viewModel.AddProduct(product);
            SearchBox.Focus();
        }
    }
}
