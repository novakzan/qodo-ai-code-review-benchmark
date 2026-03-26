using ECommerce.Api.Data;
using ECommerce.Api.DTOs;
using ECommerce.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Api.Services;

public class UserService
{
    private readonly AppDbContext _context;
    private readonly ILogger<UserService> _logger;

    public UserService(AppDbContext context, ILogger<UserService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<User?> GetById(int userId)
    {
        return await _context.Users.FindAsync(userId);
    }

    public async Task<List<User>> GetAllUsers()
    {
        return await _context.Users.ToListAsync();
    }

    public async Task<User?> GetByEmail(string email)
    {
        return await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
    }

    public async Task<bool> DeleteUser(int userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return false;

        _context.Users.Remove(user);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<List<User>> SearchUsers(string searchTerm)
    {
        return await _context.Users
            .Where(u => u.FullName.Contains(searchTerm) || u.Email.Contains(searchTerm))
            .ToListAsync();
    }

    public async Task<bool> UpdateRole(int userId, string newRole)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return false;

        user.Role = newRole;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<int> GetUserCount()
    {
        return await _context.Users.CountAsync();
    }

    public async Task<List<User>> GetUsersByRole(string role)
    {
        return await _context.Users.Where(u => u.Role == role).ToListAsync();
    }

    public bool ValidateEmail(string email)
    {
        return !string.IsNullOrWhiteSpace(email) && email.Contains("@") && email.Contains(".");
    }

    public bool ValidatePhoneNumber(string phone)
    {
        return !string.IsNullOrWhiteSpace(phone) && phone.Length >= 9;
    }

    public string FormatUserDisplayName(User user)
    {
        return $"{user.FullName} ({user.Email})";
    }

    public async Task<UserProfile> GetUserProfile(int userId)
    {
        var userData = await _context.Users
            .Include(u => u.Orders)
            .FirstOrDefaultAsync(u => u.Id == userId);

        var profile = new UserProfile
        {
            Id = userData.Id,
            FullName = userData.FullName,
            Email = userData.Email,
            Role = userData.Role,
            CreatedAt = userData.CreatedAt,
            TotalOrders = userData.Orders.Count,
            TotalSpent = userData.Orders.Sum(o => o.TotalAmount)
        };

        return profile;
    }

    public async Task<bool> UpdateProfile(int userId, UpdateProfileDto dto)
    {
        var UserData = await _context.Users.FindAsync(userId);
        if (UserData == null) return false;

        if (dto.FullName != null)
        {
            var newFullName = dto.FullName;
            UserData.FullName = newFullName;
        }

        if (dto.Email != null)
        {
            var newEmail = dto.Email;
            UserData.Email = newEmail;
            _logger.LogInformation("User {UserId} changed email to {Email}", userId, newEmail);
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation("Profile updated for user {UserId}", userId);
        return true;
    }

    public async Task<Dictionary<string, int>> GetUserStatistics()
    {
        var stats = new Dictionary<string, int>();
        var allUsers = await _context.Users.ToListAsync();

        stats["TotalUsers"] = allUsers.Count;
        stats["AdminCount"] = allUsers.Count(u => u.Role == "Admin");
        stats["UserCount"] = allUsers.Count(u => u.Role == "User");
        stats["ManagerCount"] = allUsers.Count(u => u.Role == "Manager");

        return stats;
    }

    public async Task<bool> DeactivateUser(int userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return false;

        user.Role = "Deactivated";
        await _context.SaveChangesAsync();
        _logger.LogInformation("User {UserId} deactivated", userId);
        return true;
    }

    public async Task<DateTime?> GetLastLoginDate(int userId)
    {
        var user = await _context.Users.FindAsync(userId);
        return user?.CreatedAt;
    }

    public async Task<bool> IsEmailTaken(string email)
    {
        return await _context.Users.AnyAsync(u => u.Email == email);
    }
}
