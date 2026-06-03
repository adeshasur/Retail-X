using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using RetailX.Models;
using RetailX.ViewModels;

namespace RetailX.Views;

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
            FocusScannerInput();
            e.Handled = true;
        }
    }

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.F1)
        {
            FocusScannerInput();
            e.Handled = true;
            return;
        }

        if (e.Key == Key.F2)
        {
            BeginCartEdit(3);
            e.Handled = true;
            return;
        }

        if (e.Key == Key.F3)
        {
            BeginCartEdit(4);
            e.Handled = true;
        }
    }

    private void OnSearchResultDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is ListBox { SelectedItem: Product product } && DataContext is PosViewModel viewModel)
        {
            viewModel.AddProduct(product);
            FocusScannerInput();
        }
    }

    private void FocusScannerInput()
    {
        Dispatcher.BeginInvoke(() =>
        {
            SearchBox.Focus();
            Keyboard.Focus(SearchBox);
            SearchBox.SelectAll();
        }, DispatcherPriority.Input);
    }

    private void BeginCartEdit(int columnIndex)
    {
        if (CartGrid.Items.Count == 0)
        {
            FocusScannerInput();
            return;
        }

        if (CartGrid.SelectedItem is null)
        {
            CartGrid.SelectedIndex = CartGrid.Items.Count - 1;
        }

        CartGrid.Focus();
        CartGrid.CurrentCell = new DataGridCellInfo(CartGrid.SelectedItem, CartGrid.Columns[columnIndex]);
        CartGrid.BeginEdit();
    }
}
