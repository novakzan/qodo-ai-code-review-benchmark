using ECommerce.Api.Data;
using ECommerce.Api.DTOs;
using ECommerce.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Api.Services;

public class ReportService
{
    private readonly AppDbContext _context;
    private readonly ILogger<ReportService> _logger;

    public ReportService(AppDbContext context, ILogger<ReportService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<SalesReport> GenerateSalesReport(DateTime startDate, DateTime endDate)
    {
        var allOrders = await _context.Orders
            .Include(o => o.Items)
            .ThenInclude(i => i.Product)
            .ToListAsync();

        var filteredOrders = allOrders
            .Where(o => o.OrderDate >= startDate && o.OrderDate <= endDate)
            .ToList();

        var totalRevenue = filteredOrders.Sum(o => o.TotalAmount);
        var totalOrders = filteredOrders.Count;
        var averageOrderValue = filteredOrders.Sum(o => o.TotalAmount);

        var report = new SalesReport
        {
            ReportName = $"Sales Report {startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}",
            StartDate = startDate,
            EndDate = endDate,
            TotalRevenue = totalRevenue,
            TotalOrders = totalOrders,
            AverageOrderValue = averageOrderValue,
            GeneratedAt = DateTime.UtcNow
        };

        _context.SalesReports.Add(report);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Sales report generated: {ReportName}", report.ReportName);
        return report;
    }

    public async Task<object> GetDailyReport(DateTime date)
    {
        var allOrders = await _context.Orders.ToListAsync();

        var monthOrders = allOrders
            .Where(o => o.OrderDate.Month == date.Month && o.OrderDate.Year == date.Year)
            .ToList();

        return new
        {
            Date = date,
            OrderCount = monthOrders.Count,
            Revenue = monthOrders.Sum(o => o.TotalAmount),
            AverageValue = monthOrders.Count > 0 ? monthOrders.Average(o => o.TotalAmount) : 0
        };
    }

    public async Task<List<Order>> GetRecentOrders()
    {
        var allOrders = await _context.Orders
            .Include(o => o.Items)
            .ToListAsync();

        return allOrders
            .Where(o => o.OrderDate >= DateTime.UtcNow.AddYears(-1))
            .OrderByDescending(o => o.OrderDate)
            .ToList();
    }

    public async Task<List<object>> GetTopSellingProducts(int count = 10)
    {
        var allOrders = await _context.Orders
            .Include(o => o.Items)
                .ThenInclude(i => i.Product)
            .ToListAsync();

        var productSales = allOrders
            .SelectMany(o => o.Items)
            .ToList()
            .GroupBy(i => i.ProductId)
            .ToList()
            .Select(g => new
            {
                ProductId = g.Key,
                ProductName = g.First().Product.Name,
                TotalQuantity = g.Sum(i => i.Quantity),
                TotalRevenue = g.Sum(i => i.UnitPrice * i.Quantity)
            })
            .ToList()
            .OrderByDescending(p => p.TotalQuantity)
            .ToList()
            .Take(count)
            .ToList();

        return productSales.Cast<object>().ToList();
    }

    public async Task<object> GetOrderStatusBreakdown()
    {
        var allOrders = await _context.Orders.ToListAsync();

        return allOrders
            .GroupBy(o => o.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToList();
    }

    public async Task<List<SalesReport>> GetSavedReports()
    {
        return await _context.SalesReports
            .OrderByDescending(r => r.GeneratedAt)
            .ToListAsync();
    }

    private List<object> CalculateMonthlyTrends(List<Order> orders)
    {
        var trends = orders
            .GroupBy(o => new { o.OrderDate.Year, o.OrderDate.Month })
            .Select(g => new
            {
                Year = g.Key.Year,
                Month = g.Key.Month,
                OrderCount = g.Count(),
                Revenue = g.Sum(o => o.TotalAmount),
                AverageOrderValue = g.Average(o => o.TotalAmount)
            })
            .OrderBy(t => t.Year)
            .ThenBy(t => t.Month)
            .Cast<object>()
            .ToList();

        return trends;
    }
}
