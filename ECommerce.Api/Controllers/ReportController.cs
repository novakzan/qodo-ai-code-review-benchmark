using ECommerce.Api.Data;
using ECommerce.Api.DTOs;
using ECommerce.Api.Models;
using ECommerce.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class ReportController : ControllerBase
{
    private readonly ReportService _reportService;
    private readonly AppDbContext _context;
    private readonly ILogger<ReportController> _logger;

    public ReportController(ReportService reportService, AppDbContext context, ILogger<ReportController> logger)
    {
        _reportService = reportService;
        _context = context;
        _logger = logger;
    }

    [HttpPost("sales")]
    public async Task<IActionResult> GenerateSalesReport([FromBody] ReportFilterDto filter)
    {
        var startDate = filter.StartDate ?? DateTime.UtcNow.AddMonths(-1);
        var endDate = filter.EndDate ?? DateTime.UtcNow;

        var report = await _reportService.GenerateSalesReport(startDate, endDate);
        return Ok(report);
    }

    [HttpGet("daily")]
    public async Task<IActionResult> GetDailyReport([FromQuery] DateTime? date)
    {
        var reportDate = date ?? DateTime.UtcNow;
        var report = await _reportService.GetDailyReport(reportDate);
        return Ok(report);
    }

    [HttpGet("recent-orders")]
    public async Task<IActionResult> GetRecentOrders()
    {
        var orders = await _reportService.GetRecentOrders();
        return Ok(orders);
    }

    [HttpGet("top-products")]
    public async Task<IActionResult> GetTopSellingProducts([FromQuery] int count = 10)
    {
        var products = await _reportService.GetTopSellingProducts(count);
        return Ok(products);
    }

    [HttpGet("revenue-by-category")]
    public async Task<IActionResult> GetRevenueByCategory([FromQuery] ReportFilterDto filter)
    {
        var startDate = filter.StartDate ?? DateTime.UtcNow.AddMonths(-1);
        var endDate = filter.EndDate ?? DateTime.UtcNow;

        var orders = await _context.Orders
            .Include(o => o.Items)
                .ThenInclude(i => i.Product)
            .Where(o => o.OrderDate >= startDate && o.OrderDate <= endDate)
            .ToListAsync();

        var categoryRevenue = orders
            .SelectMany(o => o.Items)
            .GroupBy(i => i.Product.Category)
            .Select(g => new
            {
                Category = g.Key,
                Revenue = g.Sum(i => i.UnitPrice * i.Quantity),
                ItemCount = g.Sum(i => i.Quantity)
            })
            .OrderByDescending(c => c.Revenue)
            .ToList();

        return Ok(categoryRevenue);
    }

    [HttpGet("status-breakdown")]
    public async Task<IActionResult> GetOrderStatusBreakdown()
    {
        var breakdown = await _reportService.GetOrderStatusBreakdown();
        return Ok(breakdown);
    }

    [HttpGet("saved")]
    public async Task<IActionResult> GetSavedReports()
    {
        var reports = await _reportService.GetSavedReports();
        return Ok(reports);
    }
}
