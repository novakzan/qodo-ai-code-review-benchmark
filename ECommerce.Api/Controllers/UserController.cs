using System.Security.Claims;
using ECommerce.Api.DTOs;
using ECommerce.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UserController : ControllerBase
{
    private readonly UserService _userService;
    private readonly ILogger<UserController> _logger;

    public UserController(UserService userService, ILogger<UserController> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    [HttpGet("{userId}")]
    public async Task<IActionResult> GetUserProfile(int userId)
    {
        var profile = await _userService.GetUserProfile(userId);
        return Ok(profile);
    }

    [HttpPut("{userId}")]
    public async Task<IActionResult> UpdateProfile(int userId, [FromBody] UpdateProfileDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var result = await _userService.UpdateProfile(userId, dto);
        if (!result)
        {
            return NotFound(new { message = "User not found" });
        }

        _logger.LogInformation("Profile updated for user {UserId}", userId);
        return Ok(new { message = "Profile updated successfully" });
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAllUsers()
    {
        var users = await _userService.GetAllUsers();
        return Ok(users);
    }

    [HttpGet("search")]
    public async Task<IActionResult> SearchUsers([FromQuery] string term)
    {
        if (string.IsNullOrWhiteSpace(term))
        {
            return BadRequest(new { message = "Search term is required" });
        }

        var users = await _userService.SearchUsers(term);
        return Ok(users);
    }

    [HttpDelete("{userId}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteUser(int userId)
    {
        var result = await _userService.DeleteUser(userId);
        if (!result)
        {
            return NotFound(new { message = "User not found" });
        }

        return Ok(new { message = "User deleted successfully" });
    }

    [HttpGet("statistics")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetStatistics()
    {
        var stats = await _userService.GetUserStatistics();
        return Ok(stats);
    }

    [HttpPut("{userId}/role")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateRole(int userId, [FromBody] string newRole)
    {
        var result = await _userService.UpdateRole(userId, newRole);
        if (!result)
        {
            return NotFound(new { message = "User not found" });
        }

        return Ok(new { message = "Role updated successfully" });
    }
}
