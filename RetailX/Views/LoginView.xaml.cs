using System.Windows.Controls;
using System.Windows.Input;
using RetailX.ViewModels;

namespace RetailX.Views;

public partial class LoginView : UserControl
{
    public LoginView()
    {
        InitializeComponent();
    }

    private void OnPasswordChanged(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is LoginViewModel viewModel)
        {
            viewModel.Password = PasswordInput.Password;
        }
    }

    private void OnPasswordKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && DataContext is LoginViewModel viewModel)
        {
            viewModel.LoginCommand.Execute(null);
            e.Handled = true;
        }
    }
}
