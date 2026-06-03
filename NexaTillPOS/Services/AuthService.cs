using Microsoft.EntityFrameworkCore;
using NexaTillPOS.Data;
using NexaTillPOS.Models;

namespace NexaTillPOS.Services;

public class AuthService
{
    public User? CurrentUser { get; private set; }

    public async Task<User?> LoginAsync(string username, string password)
    {
        await using var db = new PosDbContext();
        var user = await db.Users.FirstOrDefaultAsync(x => x.Username == username.Trim() && x.IsActive);

        if (user is null || !PasswordHasher.Verify(password, user.PasswordHash))
        {
            return null;
        }

        CurrentUser = user;
        return user;
    }

    public void Logout()
    {
        CurrentUser = null;
    }
}
