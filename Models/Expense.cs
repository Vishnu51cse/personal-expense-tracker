using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PersonalExpenseTracker.Models
{
    /// <summary>
    /// Represents an individual expense record belonging to a specific user.
    /// </summary>
    public class Expense
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Range(0.01, 10000000.00, ErrorMessage = "Amount must be greater than zero.")]
        public decimal Amount { get; set; }

        [Required]
        [MaxLength(50)]
        public string Category { get; set; } = "Other";

        [Required]
        public DateTime Date { get; set; } = DateTime.Today;

        [MaxLength(500)]
        public string? Description { get; set; }

        // Foreign Key to User
        [Required]
        public int UserId { get; set; }

        // Navigation property
        [ForeignKey(nameof(UserId))]
        public User? User { get; set; }
    }
}
