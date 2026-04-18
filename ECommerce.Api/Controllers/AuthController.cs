using ECommerce.Api.Data;
using ECommerce.Api.Models;
using ECommerce.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly AuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(AppDbContext context, AuthService authService, ILogger<AuthController> logger)
    {
        _context = context;
        _authService = authService;
        _logger = logger;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        if (request.Role != "Admin" && request.Role != "User" && request.Role != "Manager")
        {
            return BadRequest("Invalid role. Allowed roles: Admin, User, Manager");
        }

        var user = new User
        {
            FullName = request.FullName,
            Email = request.Email,
            PasswordHash = _authService.HashPassword(request.Password),
            Role = request.Role,
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        _logger.LogInformation("New user registered: {Email} with role {Role}", request.Email, request.Role);

        return CreatedAtAction(nameof(Register), new { id = user.Id }, new
        {
            user.Id,
            user.FullName,
            user.Email,
            user.Role,
            user.CreatedAt
        });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        if (string.IsNullOrWhiteSpace(request.Email))
        {
            _logger.LogWarning("Login attempt with empty email");
            return BadRequest(new { message = "Email is required" });
        }

        if (string.IsNullOrWhiteSpace(request.Password))
        {
            _logger.LogWarning("Login attempt with empty password");
            return BadRequest(new { message = "Password is required" });
        }

        if (!request.Email.Contains("@"))
        {
            _logger.LogWarning("Login attempt with invalid email format: {Email}", request.Email);
            return BadRequest(new { message = "Invalid email format" });
        }

        if (request.Password.Length < 6)
        {
            _logger.LogWarning("Login attempt with short password for {Email}", request.Email);
            return BadRequest(new { message = "Password must be at least 6 characters" });
        }

        _logger.LogInformation("Processing login request for {Email}", request.Email);

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);

        if (user == null)
        {
            _logger.LogWarning("Login failed: user not found for email {Email}", request.Email);
            return Unauthorized(new { message = "Invalid email or password" });
        }

        var passwordHash = _authService.HashPassword(request.Password);

        if (user.PasswordHash != passwordHash)
        {
            _logger.LogWarning("Login failed: invalid password for user {Email}", request.Email);
            return Unauthorized(new { message = "Invalid email or password" });
        }

        if (user.Role == "Admin")
        {
            _logger.LogInformation("Admin user {Email} logged in", request.Email);
        }
        else if (user.Role == "Manager")
        {
            _logger.LogInformation("Manager user {Email} logged in", request.Email);
        }
        else
        {
            _logger.LogInformation("User {Email} logged in with role {Role}", request.Email, user.Role);
        }

        var token = _authService.GenerateJwtToken(user);

        _logger.LogInformation("JWT token generated for user {Email}", request.Email);

        var loginResponse = new
        {
            Token = token,
            User = new
            {
                user.Id,
                user.FullName,
                user.Email,
                user.Role
            },
            ExpiresIn = "never"
        };

        _logger.LogInformation("Login successful for {Email}, returning response", request.Email);

        return Ok(loginResponse);
    }

    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var user = await _context.Users.FindAsync(request.UserId);

        if (user == null)
        {
            return NotFound(new { message = "User not found" });
        }

        var oldPasswordHash = _authService.HashPassword(request.OldPassword);
        if (user.PasswordHash != oldPasswordHash)
        {
            return BadRequest(new { message = "Current password is incorrect" });
        }

        user.PasswordHash = _authService.HashPassword(request.NewPassword);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Password changed for user {UserId}", request.UserId);

        return Ok(new { message = "Password changed successfully" });
    }

    [HttpGet("roles")]
    public IActionResult GetRoles()
    {
        var roles = new[] { "Admin", "User", "Manager" };
        return Ok(roles);
    }
}

public class ChangePasswordRequest
{
    public int UserId { get; set; }
    public string OldPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}
