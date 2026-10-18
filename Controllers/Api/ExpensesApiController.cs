using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PersonalExpenseTracker.DTOs;
using PersonalExpenseTracker.Services;

namespace PersonalExpenseTracker.Controllers.Api
{
    [ApiController]
    [Route("api/expenses")]
    [Produces("application/json")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class ExpensesApiController : ControllerBase
    {
        private readonly IExpenseService _expenseService;

        public ExpensesApiController(IExpenseService expenseService)
        {
            _expenseService = expenseService;
        }

        private int GetUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(claim, out var id) ? id : 0;
        }

        /// <summary>
        /// Retrieves all expenses for the authenticated user, optionally filtered by category.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetExpenses([FromQuery] string? category = null)
        {
            var userId = GetUserId();
            if (userId == 0) return Unauthorized(new { message = "Invalid user identity in token." });

            var expenses = await _expenseService.GetExpensesByUserAsync(userId, category);
            return Ok(expenses);
        }

        /// <summary>
        /// Retrieves a single expense by ID for the authenticated user.
        /// </summary>
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetExpenseById(int id)
        {
            var userId = GetUserId();
            if (userId == 0) return Unauthorized(new { message = "Invalid user identity in token." });

            var expense = await _expenseService.GetExpenseByIdAsync(id, userId);
            if (expense == null)
            {
                return NotFound(new { message = $"Expense #{id} not found or you do not have permission to view it." });
            }

            return Ok(expense);
        }

        /// <summary>
        /// Creates a new expense entry for the authenticated user.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateExpense([FromBody] CreateExpenseDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userId = GetUserId();
            if (userId == 0) return Unauthorized(new { message = "Invalid user identity in token." });

            var created = await _expenseService.CreateExpenseAsync(dto, userId);
            return CreatedAtAction(nameof(GetExpenseById), new { id = created.Id }, created);
        }

        /// <summary>
        /// Updates an existing expense entry belonging to the authenticated user.
        /// </summary>
        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateExpense(int id, [FromBody] UpdateExpenseDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userId = GetUserId();
            if (userId == 0) return Unauthorized(new { message = "Invalid user identity in token." });

            var success = await _expenseService.UpdateExpenseAsync(id, dto, userId);
            if (!success)
            {
                return NotFound(new { message = $"Expense #{id} not found or you do not have permission to edit it." });
            }

            return Ok(new { message = "Expense updated successfully." });
        }

        /// <summary>
        /// Deletes an expense entry belonging to the authenticated user.
        /// </summary>
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteExpense(int id)
        {
            var userId = GetUserId();
            if (userId == 0) return Unauthorized(new { message = "Invalid user identity in token." });

            var success = await _expenseService.DeleteExpenseAsync(id, userId);
            if (!success)
            {
                return NotFound(new { message = $"Expense #{id} not found or you do not have permission to delete it." });
            }

            return Ok(new { message = "Expense deleted successfully." });
        }

        /// <summary>
        /// Retrieves total expenses and category summary for the authenticated user.
        /// </summary>
        [HttpGet("summary")]
        public async Task<IActionResult> GetSummary()
        {
            var userId = GetUserId();
            if (userId == 0) return Unauthorized(new { message = "Invalid user identity in token." });

            var summary = await _expenseService.GetSummaryByUserAsync(userId);
            return Ok(summary);
        }

        /// <summary>
        /// Returns the supported expense categories.
        /// </summary>
        [HttpGet("categories")]
        [AllowAnonymous]
        public IActionResult GetCategories()
        {
            var categories = _expenseService.GetPredefinedCategories();
            return Ok(categories);
        }
    }
}
