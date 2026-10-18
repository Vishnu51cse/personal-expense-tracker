using Microsoft.EntityFrameworkCore;
using PersonalExpenseTracker.Data;
using PersonalExpenseTracker.DTOs;
using PersonalExpenseTracker.Models;

namespace PersonalExpenseTracker.Services
{
    /// <summary>
    /// Implements expense business logic with strict user-level data isolation.
    /// </summary>
    public class ExpenseService : IExpenseService
    {
        private readonly ApplicationDbContext _context;

        private static readonly List<string> PredefinedCategories = new()
        {
            "Food & Dining",
            "Transportation",
            "Housing & Rent",
            "Utilities",
            "Entertainment",
            "Healthcare",
            "Shopping",
            "Education",
            "Personal Care",
            "Other"
        };

        public ExpenseService(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<string> GetPredefinedCategories() => PredefinedCategories;

        public async Task<IEnumerable<ExpenseResponseDto>> GetExpensesByUserAsync(int userId, string? category = null)
        {
            var query = _context.Expenses
                .AsNoTracking()
                .Where(e => e.UserId == userId);

            if (!string.IsNullOrWhiteSpace(category) && category != "All")
            {
                query = query.Where(e => e.Category == category);
            }

            return await query
                .OrderByDescending(e => e.Date)
                .ThenByDescending(e => e.Id)
                .Select(e => new ExpenseResponseDto
                {
                    Id = e.Id,
                    Title = e.Title,
                    Amount = e.Amount,
                    Category = e.Category,
                    Date = e.Date,
                    Description = e.Description,
                    UserId = e.UserId
                })
                .ToListAsync();
        }

        public async Task<ExpenseResponseDto?> GetExpenseByIdAsync(int id, int userId)
        {
            var expense = await _context.Expenses
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId);

            if (expense == null) return null;

            return new ExpenseResponseDto
            {
                Id = expense.Id,
                Title = expense.Title,
                Amount = expense.Amount,
                Category = expense.Category,
                Date = expense.Date,
                Description = expense.Description,
                UserId = expense.UserId
            };
        }

        public async Task<ExpenseResponseDto> CreateExpenseAsync(CreateExpenseDto dto, int userId)
        {
            var expense = new Expense
            {
                Title = dto.Title.Trim(),
                Amount = dto.Amount,
                Category = string.IsNullOrWhiteSpace(dto.Category) ? "Other" : dto.Category.Trim(),
                Date = dto.Date,
                Description = dto.Description?.Trim(),
                UserId = userId
            };

            _context.Expenses.Add(expense);
            await _context.SaveChangesAsync();

            return new ExpenseResponseDto
            {
                Id = expense.Id,
                Title = expense.Title,
                Amount = expense.Amount,
                Category = expense.Category,
                Date = expense.Date,
                Description = expense.Description,
                UserId = expense.UserId
            };
        }

        public async Task<bool> UpdateExpenseAsync(int id, UpdateExpenseDto dto, int userId)
        {
            var expense = await _context.Expenses
                .FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId);

            if (expense == null) return false;

            expense.Title = dto.Title.Trim();
            expense.Amount = dto.Amount;
            expense.Category = string.IsNullOrWhiteSpace(dto.Category) ? "Other" : dto.Category.Trim();
            expense.Date = dto.Date;
            expense.Description = dto.Description?.Trim();

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteExpenseAsync(int id, int userId)
        {
            var expense = await _context.Expenses
                .FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId);

            if (expense == null) return false;

            _context.Expenses.Remove(expense);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<ExpenseSummaryDto> GetSummaryByUserAsync(int userId)
        {
            var userExpenses = await _context.Expenses
                .AsNoTracking()
                .Where(e => e.UserId == userId)
                .ToListAsync();

            var totalAmount = userExpenses.Sum(e => e.Amount);
            var totalCount = userExpenses.Count;

            var currentMonth = DateTime.UtcNow.Month;
            var currentYear = DateTime.UtcNow.Year;
            var thisMonthTotal = userExpenses
                .Where(e => e.Date.Month == currentMonth && e.Date.Year == currentYear)
                .Sum(e => e.Amount);

            var categoryBreakdown = userExpenses
                .GroupBy(e => e.Category)
                .Select(g => new CategoryTotalDto
                {
                    Category = g.Key,
                    TotalAmount = g.Sum(x => x.Amount),
                    Count = g.Count(),
                    Percentage = totalAmount > 0 ? (double)(g.Sum(x => x.Amount) / totalAmount * 100) : 0
                })
                .OrderByDescending(c => c.TotalAmount)
                .ToList();

            return new ExpenseSummaryDto
            {
                TotalAmount = totalAmount,
                TotalCount = totalCount,
                ThisMonthTotal = thisMonthTotal,
                CategoryBreakdown = categoryBreakdown
            };
        }
    }
}
