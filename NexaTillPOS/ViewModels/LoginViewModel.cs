using NexaTillPOS.Models;
using NexaTillPOS.Services;

namespace NexaTillPOS.ViewModels;

public class LoginViewModel : ObservableObject
{
    private readonly AuthService _authService;
    private readonly Action<User> _onLoggedIn;
    private string _username = "admin";
    private string _password = string.Empty;
    private string _errorMessage = string.Empty;

    public LoginViewModel(AuthService authService, Action<User> onLoggedIn)
    {
        _authService = authService;
        _onLoggedIn = onLoggedIn;
        LoginCommand = new AsyncRelayCommand(LoginAsync);
    }

    public string Username
    {
        get => _username;
        set => SetProperty(ref _username, value);
    }

    public string Password
    {
        get => _password;
        set => SetProperty(ref _password, value);
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    public AsyncRelayCommand LoginCommand { get; }

    private async Task LoginAsync()
    {
        ErrorMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = "Enter username and password.";
            return;
        }

        var user = await _authService.LoginAsync(Username, Password);
        if (user is null)
        {
            ErrorMessage = "Invalid login or inactive user.";
            return;
        }

        _onLoggedIn(user);
    }
}
