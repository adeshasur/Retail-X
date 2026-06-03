# NexaTill POS by CodaWix

Initial runnable WPF/.NET 8 supermarket POS starter.

## Run

```powershell
dotnet restore NexaTillPOS.slnx
dotnet build NexaTillPOS.slnx
dotnet run --project NexaTillPOS\NexaTillPOS.csproj
```

Default development login:

- Username: `admin`
- Password: `admin123`

The SQLite database is created automatically at:

`%LOCALAPPDATA%\CodaWix\NexaTillPOS\nexatill.db`

## Structure

- `NexaTillPOS/Models` - EF Core domain entities and enums.
- `NexaTillPOS/Data` - SQLite `DbContext` and development database seeding.
- `NexaTillPOS/Services` - authentication, password hashing, product search, and sale completion.
- `NexaTillPOS/ViewModels` - MVVM state, commands, POS cart logic, shell navigation, reports, and placeholders.
- `NexaTillPOS/Views` - WPF XAML screens for login, shell, POS, product list, reports, and placeholders.
- `NexaTillPOS/Controls` - WPF helper controls/attached properties.
- `NexaTillPOS/Converters` - XAML value converters.
