using System.ComponentModel.DataAnnotations;

namespace ECommerce.Api.DTOs;

public class UpdateProfileDto
{
    [MaxLength(100)]
    public string? FullName { get; set; }

    [EmailAddress]
    [MaxLength(200)]
    public string? Email { get; set; }

    [MaxLength(20)]
    public string? PhoneNumber { get; set; }

    [MaxLength(500)]
    public string? Address { get; set; }
}
