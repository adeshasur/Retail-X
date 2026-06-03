using System.ComponentModel.DataAnnotations;

namespace RetailX.Models;

public class Category
{
    public int Id { get; set; }

    [MaxLength(80)]
    public string Name { get; set; } = string.Empty;

    public List<Product> Products { get; set; } = [];
}
