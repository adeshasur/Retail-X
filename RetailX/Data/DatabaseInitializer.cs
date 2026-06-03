using Microsoft.EntityFrameworkCore;
using RetailX.Models;
using RetailX.Services;

namespace RetailX.Data;

public static class DatabaseInitializer
{
    public static async Task InitializeAsync()
    {
        await using var db = new PosDbContext();
        await db.Database.EnsureCreatedAsync();

        if (!await db.AppSettings.AnyAsync())
        {
            db.AppSettings.Add(new AppSetting());
        }

        var adminUser = await db.Users.FirstOrDefaultAsync(x => x.Username == "admin");
        if (adminUser is null)
        {
            db.Users.Add(new User
            {
                FullName = "System Administrator",
                Username = "admin",
                PasswordHash = PasswordHasher.Hash("admin123"),
                Role = UserRole.Admin,
                IsActive = true
            });
        }
        else
        {
            adminUser.FullName = "System Administrator";
            adminUser.PasswordHash = PasswordHasher.Hash("admin123");
            adminUser.Role = UserRole.Admin;
            adminUser.IsActive = true;
        }

        if (!await db.Categories.AnyAsync())
        {
            var grocery = new Category { Name = "Grocery" };
            var dairy = new Category { Name = "Dairy" };
            var household = new Category { Name = "Household" };
            var beverages = new Category { Name = "Beverages" };
            var produce = new Category { Name = "Fresh Produce" };

            db.Categories.AddRange(grocery, dairy, household, beverages, produce);
            await db.SaveChangesAsync();

            db.Products.AddRange(
                Product("4791001000011", "RIC-SUD-005", "Sudu Kekulu Rice 5kg", grocery.Id, "Nexa Select", 1450, 1675, 86, 12),
                Product("4791001000028", "DAL-MYS-001", "Mysore Dhal 1kg", grocery.Id, "Island Pantry", 520, 645, 120, 20),
                Product("4791001000035", "SUG-WHT-001", "White Sugar 1kg", grocery.Id, "Ceylon Staples", 290, 365, 98, 15),
                Product("4791001000042", "MLK-PWD-400", "Full Cream Milk Powder 400g", dairy.Id, "Highland Valley", 980, 1160, 54, 10),
                Product("4791001000059", "BIS-CRM-100", "Cream Crackers 100g", grocery.Id, "Lanka Bakes", 135, 180, 160, 30),
                Product("4791001000066", "SOP-HRB-090", "Herbal Soap 90g", household.Id, "Pure Leaf", 85, 130, 140, 25),
                Product("4791001000073", "NDL-CHK-075", "Chicken Noodles 75g", grocery.Id, "Quick Bowl", 95, 135, 210, 40),
                Product("4791001000080", "TEA-BLK-200", "Ceylon Black Tea 200g", beverages.Id, "Hill Crest", 610, 760, 42, 8),
                Product("4791001000097", "SFT-COL-500", "Cola Soft Drink 500ml", beverages.Id, "FizzUp", 120, 180, 72, 18),
                Product("4791001000103", "VEG-CAR-001", "Carrot 1kg", produce.Id, "Fresh Market", 310, 430, 35, 8),
                Product("4791001000110", "VEG-TOM-001", "Tomato 1kg", produce.Id, "Fresh Market", 260, 390, 28, 8),
                Product("4791001000127", "RIC-BAS-001", "Basmati Rice 1kg", grocery.Id, "Golden Grain", 760, 930, 64, 12)
            );
        }

        await db.SaveChangesAsync();
    }

    private static Product Product(
        string barcode,
        string sku,
        string name,
        int categoryId,
        string brand,
        decimal cost,
        decimal price,
        decimal stock,
        decimal reorderLevel)
    {
        return new Product
        {
            Barcode = barcode,
            SKU = sku,
            Name = name,
            CategoryId = categoryId,
            Brand = brand,
            CostPrice = cost,
            SellingPrice = price,
            StockQuantity = stock,
            ReorderLevel = reorderLevel,
            IsActive = true
        };
    }
}
