using System.ComponentModel.DataAnnotations;

namespace PersonalExpenseTracker.Models
{
    /// <summary>
    /// Represents a registered user in the Expense Tracker system.
    /// </summary>
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [MaxLength(100)]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation property for related expenses
        public ICollection<Expense> Expenses { get; set; } = new List<Expense>();
    }
}
