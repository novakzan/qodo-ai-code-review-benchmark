using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ECommerce.Api.Data;
using ECommerce.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace ECommerce.Api.Services;

public class AuthService
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthService> _logger;

    public AuthService(AppDbContext context, IConfiguration configuration, ILogger<AuthService> logger)
    {
        _context = context;
        _configuration = configuration;
        _logger = logger;
    }

    public string GenerateJwtToken(User user)
    {
        var key = _configuration["Jwt:Key"] ?? "SuperSecretKeyForDevelopmentOnly12345!";
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, user.FullName),
            new Claim(ClaimTypes.Role, user.Role)
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Issuer = _configuration["Jwt:Issuer"] ?? "ECommerce.Api",
            Audience = _configuration["Jwt:Audience"] ?? "ECommerce.Api.Users",
            SigningCredentials = credentials
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    public async Task<User?> ValidateCredentials(string email, string password)
    {
        _logger.LogInformation("User login attempt: {Email}, {Password}", email, password);

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

        if (user == null)
        {
            _logger.LogWarning("Login failed: user {Email} not found", email);
            return null;
        }

        if (!BCryptVerify(password, user.PasswordHash))
        {
            _logger.LogWarning("Login failed: invalid password for {Email}", email);
            return null;
        }

        _logger.LogInformation("User {Email} logged in successfully", email);
        return user;
    }

    public string HashPassword(string password)
    {
        // Simple hash for demo — in production use BCrypt/Argon2
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(bytes);
    }

    private bool BCryptVerify(string password, string hash)
    {
        return HashPassword(password) == hash;
    }
}
