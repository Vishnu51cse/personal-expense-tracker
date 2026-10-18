using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PersonalExpenseTracker.Models;

namespace PersonalExpenseTracker.Data
{
    /// <summary>
    /// Utility class to seed initial demo data if the database is empty.
    /// </summary>
    public static class DbInitializer
    {
        public static async Task SeedAsync(ApplicationDbContext context)
        {
            // Ensure database is created
            await context.Database.MigrateAsync();

            // Check if users already exist
            if (await context.Users.AnyAsync())
            {
                return; // Database already seeded or populated
            }

            var passwordHasher = new PasswordHasher<User>();

            // 1. Create Demo User
            var demoUser = new User
            {
                Username = "demouser",
                Email = "demo@example.com",
                CreatedAt = DateTime.UtcNow
            };
            demoUser.PasswordHash = passwordHasher.HashPassword(demoUser, "Password123!");

            context.Users.Add(demoUser);
            await context.SaveChangesAsync();

            // 2. Create Initial Sample Expenses
            var expenses = new List<Expense>
            {
                new()
                {
                    Title = "Weekly Groceries",
                    Amount = 142.50m,
                    Category = "Food & Dining",
                    Date = DateTime.Today.AddDays(-2),
                    Description = "Whole Foods grocery shopping",
                    UserId = demoUser.Id
                },
                new()
                {
                    Title = "Monthly Subway Pass",
                    Amount = 85.00m,
                    Category = "Transportation",
                    Date = DateTime.Today.AddDays(-5),
                    Description = "Public transit monthly pass",
                    UserId = demoUser.Id
                },
                new()
                {
                    Title = "Electricity Bill",
                    Amount = 78.25m,
                    Category = "Utilities",
                    Date = DateTime.Today.AddDays(-7),
                    Description = "City Power electric bill",
                    UserId = demoUser.Id
                },
                new()
                {
                    Title = "Movie & Dinner",
                    Amount = 56.40m,
                    Category = "Entertainment",
                    Date = DateTime.Today.AddDays(-1),
                    Description = "Weekend cinema tickets and snacks",
                    UserId = demoUser.Id
                },
                new()
                {
                    Title = "Apartment Internet",
                    Amount = 60.00m,
                    Category = "Utilities",
                    Date = DateTime.Today.AddDays(-10),
                    Description = "High-speed broadband service",
                    UserId = demoUser.Id
                }
            };

            context.Expenses.AddRange(expenses);
            await context.SaveChangesAsync();
        }
    }
}
