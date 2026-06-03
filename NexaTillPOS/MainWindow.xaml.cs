using System.Windows;

namespace NexaTillPOS;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = App.CreateAppViewModel();
    }
}
