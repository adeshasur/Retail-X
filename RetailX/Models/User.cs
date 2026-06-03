using System.ComponentModel.DataAnnotations;

namespace RetailX.Models;

public class User
{
    public int Id { get; set; }

    [MaxLength(80)]
    public string FullName { get; set; } = string.Empty;

    [MaxLength(40)]
    public string Username { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public UserRole Role { get; set; }

    public bool IsActive { get; set; } = true;
}
