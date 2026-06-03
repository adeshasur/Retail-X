using System.Windows;

namespace RetailX;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = App.CreateAppViewModel();
    }
}
