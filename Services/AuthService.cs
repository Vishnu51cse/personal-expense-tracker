using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using PersonalExpenseTracker.Data;
using PersonalExpenseTracker.DTOs;
using PersonalExpenseTracker.Models;

namespace PersonalExpenseTracker.Services
{
    /// <summary>
    /// Implements authentication logic: password hashing and JWT token issuance.
    /// Uses ASP.NET Core PasswordHasher for PBKDF2 cryptography.
    /// </summary>
    public class AuthService : IAuthService
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly PasswordHasher<User> _passwordHasher;

        public AuthService(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
            _passwordHasher = new PasswordHasher<User>();
        }

        public async Task<AuthResponseDto> RegisterAsync(RegisterDto dto)
        {
            // Normalize inputs
            var username = dto.Username.Trim();
            var email = dto.Email.Trim().ToLowerInvariant();

            // Check if username already exists
            if (await _context.Users.AnyAsync(u => u.Username.ToLower() == username.ToLower()))
            {
                return new AuthResponseDto
                {
                    Success = false,
                    Message = "Username is already taken. Please choose another."
                };
            }

            // Check if email already exists
            if (await _context.Users.AnyAsync(u => u.Email.ToLower() == email))
            {
                return new AuthResponseDto
                {
                    Success = false,
                    Message = "Email is already registered. Please login or use a different email."
                };
            }

            // Create new user entity
            var user = new User
            {
                Username = username,
                Email = email,
                CreatedAt = DateTime.UtcNow
            };

            // Hash password securely with salt using PBKDF2
            user.PasswordHash = _passwordHasher.HashPassword(user, dto.Password);

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Generate JWT for immediate use upon registration
            var token = GenerateJwtToken(user);
            var durationMinutes = _configuration.GetValue<int>("Jwt:DurationInMinutes", 120);

            return new AuthResponseDto
            {
                Success = true,
                Token = token,
                ExpiresAt = DateTime.UtcNow.AddMinutes(durationMinutes),
                UserId = user.Id,
                Username = user.Username,
                Email = user.Email,
                Message = "Registration successful!"
            };
        }

        public async Task<AuthResponseDto> LoginAsync(LoginDto dto)
        {
            var identifier = dto.UsernameOrEmail.Trim().ToLowerInvariant();

            // Find user by username or email
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username.ToLower() == identifier || u.Email.ToLower() == identifier);

            if (user == null)
            {
                return new AuthResponseDto
                {
                    Success = false,
                    Message = "Invalid username/email or password."
                };
            }

            // Verify password hash
            var verificationResult = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, dto.Password);
            if (verificationResult == PasswordVerificationResult.Failed)
            {
                return new AuthResponseDto
                {
                    Success = false,
                    Message = "Invalid username/email or password."
                };
            }

            // Generate JWT token
            var token = GenerateJwtToken(user);
            var durationMinutes = _configuration.GetValue<int>("Jwt:DurationInMinutes", 120);

            return new AuthResponseDto
            {
                Success = true,
                Token = token,
                ExpiresAt = DateTime.UtcNow.AddMinutes(durationMinutes),
                UserId = user.Id,
                Username = user.Username,
                Email = user.Email,
                Message = "Login successful!"
            };
        }

        public string GenerateJwtToken(User user)
        {
            var jwtKey = _configuration["Jwt:Key"] 
                ?? throw new InvalidOperationException("JWT Secret Key is not configured in appsettings.json.");
            var issuer = _configuration["Jwt:Issuer"] ?? "PersonalExpenseTracker";
            var audience = _configuration["Jwt:Audience"] ?? "PersonalExpenseTrackerAudience";
            var durationMinutes = _configuration.GetValue<int>("Jwt:DurationInMinutes", 120);

            var keyBytes = Encoding.UTF8.GetBytes(jwtKey);
            var securityKey = new SymmetricSecurityKey(keyBytes);
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            // Claims carry authenticated identity and user metadata
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(durationMinutes),
                Issuer = issuer,
                Audience = audience,
                SigningCredentials = credentials
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }
}
