# Retail-X by CodaWix™

Initial runnable WPF/.NET 8 supermarket POS starter.

## Run

```powershell
dotnet restore RetailX.slnx
dotnet build RetailX.slnx
dotnet run --project RetailX\RetailX.csproj
```

Default development login:

- Username: `admin`
- Password: `admin123`

The SQLite database is created automatically at:

`%LOCALAPPDATA%\CodaWix\RetailX\retailx.db`

## Structure

- `RetailX/Models` - EF Core domain entities and enums.
- `RetailX/Data` - SQLite `DbContext` and development database seeding.
- `RetailX/Services` - authentication, password hashing, product search, and sale completion.
- `RetailX/ViewModels` - MVVM state, commands, POS cart logic, shell navigation, reports, and placeholders.
- `RetailX/Views` - WPF XAML screens for login, shell, POS, product list, reports, and placeholders.
- `RetailX/Controls` - WPF helper controls/attached properties.
- `RetailX/Converters` - XAML value converters.
