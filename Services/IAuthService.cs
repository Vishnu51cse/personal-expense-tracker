using PersonalExpenseTracker.DTOs;
using PersonalExpenseTracker.Models;

namespace PersonalExpenseTracker.Services
{
    /// <summary>
    /// Service contract for user registration, authentication, and JWT token issuance.
    /// </summary>
    public interface IAuthService
    {
        Task<AuthResponseDto> RegisterAsync(RegisterDto dto);
        Task<AuthResponseDto> LoginAsync(LoginDto dto);
        string GenerateJwtToken(User user);
    }
}
