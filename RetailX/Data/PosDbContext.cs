using System.IO;
using Microsoft.EntityFrameworkCore;
using RetailX.Models;

namespace RetailX.Data;

public class PosDbContext : DbContext
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Sale> Sales => Set<Sale>();
    public DbSet<SaleItem> SaleItems => Set<SaleItem>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<InventoryTransaction> InventoryTransactions => Set<InventoryTransaction>();
    public DbSet<HeldBill> HeldBills => Set<HeldBill>();
    public DbSet<HeldBillItem> HeldBillItems => Set<HeldBillItem>();
    public DbSet<AppSetting> AppSettings => Set<AppSetting>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        var dbFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "CodaWix",
            "RetailX");

        Directory.CreateDirectory(dbFolder);
        optionsBuilder.UseSqlite($"Data Source={Path.Combine(dbFolder, "retailx.db")}");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>().HasIndex(x => x.Username).IsUnique();
        modelBuilder.Entity<Product>().HasIndex(x => x.Barcode).IsUnique();
        modelBuilder.Entity<Product>().HasIndex(x => x.SKU).IsUnique();
        modelBuilder.Entity<Sale>().HasIndex(x => x.InvoiceNumber).IsUnique();

        foreach (var entity in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entity.GetProperties().Where(p => p.ClrType == typeof(decimal) || p.ClrType == typeof(decimal?)))
            {
                property.SetPrecision(18);
                property.SetScale(2);
            }
        }
    }
}
