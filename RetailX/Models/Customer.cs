using System.ComponentModel.DataAnnotations;

namespace RetailX.Models;

public class Customer
{
    public int Id { get; set; }

    [MaxLength(120)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? Phone { get; set; }
}
