using PersonalExpenseTracker.DTOs;

namespace PersonalExpenseTracker.Services
{
    /// <summary>
    /// Service contract for managing user expenses, enforcing user-level data isolation.
    /// </summary>
    public interface IExpenseService
    {
        Task<IEnumerable<ExpenseResponseDto>> GetExpensesByUserAsync(int userId, string? category = null);
        Task<ExpenseResponseDto?> GetExpenseByIdAsync(int id, int userId);
        Task<ExpenseResponseDto> CreateExpenseAsync(CreateExpenseDto dto, int userId);
        Task<bool> UpdateExpenseAsync(int id, UpdateExpenseDto dto, int userId);
        Task<bool> DeleteExpenseAsync(int id, int userId);
        Task<ExpenseSummaryDto> GetSummaryByUserAsync(int userId);
        List<string> GetPredefinedCategories();
    }
}
