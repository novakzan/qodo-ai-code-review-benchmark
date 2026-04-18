namespace ECommerce.Api.DTOs;

public class ReportFilterDto
{
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? Status { get; set; }
    public string? Category { get; set; }
    public int? MinOrderCount { get; set; }
}
