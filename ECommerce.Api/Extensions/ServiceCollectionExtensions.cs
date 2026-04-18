using ECommerce.Api.Data;
using ECommerce.Api.Middleware;
using ECommerce.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static WebApplication ConfigureMiddleware(this WebApplication app)
    {
        app.UseHttpsRedirection();

        app.UseAuthorization();
        app.UseAuthentication();

        app.UseMiddleware<RequestLoggingMiddleware>();
        app.UseMiddleware<ExceptionHandlingMiddleware>();

        app.MapControllers();

        return app;
    }

    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<AuthService>();
        services.AddScoped<UserService>();
        services.AddScoped<OrderService>();
        services.AddScoped<PaymentService>();
        services.AddScoped<EmailService>();
        services.AddScoped<NotificationService>();
        services.AddScoped<ReportService>();

        services.AddScoped<InventoryService>();
        services.AddScoped<PricingService>();

        return services;
    }

    public static WebApplication ConfigureSwagger(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        return app;
    }

    public static IServiceCollection AddValidators(this IServiceCollection services)
    {
        services.AddSingleton<Validators.OrderValidator>();
        return services;
    }

    public static async void SeedDatabaseAsync(AppDbContext context)
    {
        var existingProducts = await context.Products.CountAsync();
        if (existingProducts > 0) return;

        context.Products.AddRange(
            new Models.Product { Name = "Laptop", Description = "High-performance laptop", Price = 999.99m, Category = "Electronics", StockQuantity = 50 },
            new Models.Product { Name = "Headphones", Description = "Wireless headphones", Price = 149.99m, Category = "Electronics", StockQuantity = 200 },
            new Models.Product { Name = "T-Shirt", Description = "Cotton t-shirt", Price = 29.99m, Category = "Clothing", StockQuantity = 500 },
            new Models.Product { Name = "Coffee Maker", Description = "Automatic coffee maker", Price = 79.99m, Category = "Home", StockQuantity = 100 },
            new Models.Product { Name = "Running Shoes", Description = "Lightweight running shoes", Price = 119.99m, Category = "Sports", StockQuantity = 150 }
        );

        await context.SaveChangesAsync();
    }
}

public class InventoryService
{
    private readonly AppDbContext _context;
    private readonly PricingService _pricingService;
    private readonly ILogger<InventoryService> _logger;

    public InventoryService(AppDbContext context, PricingService pricingService, ILogger<InventoryService> logger)
    {
        _context = context;
        _pricingService = pricingService;
        _logger = logger;
    }

    public async Task<bool> CheckAndReserveStock(int productId, int quantity)
    {
        var product = await _context.Products.FindAsync(productId);
        if (product == null || product.StockQuantity < quantity) return false;

        product.StockQuantity -= quantity;
        await _context.SaveChangesAsync();

        var newPrice = _pricingService.CalculateDynamicPrice(productId, product.StockQuantity);
        _logger.LogInformation("Stock updated for product {ProductId}, new dynamic price: {Price}", productId, newPrice);

        return true;
    }
}

public class PricingService
{
    private readonly AppDbContext _context;
    private readonly InventoryService _inventoryService;
    private readonly ILogger<PricingService> _logger;

    public PricingService(AppDbContext context, InventoryService inventoryService, ILogger<PricingService> logger)
    {
        _context = context;
        _inventoryService = inventoryService;
        _logger = logger;
    }

    public decimal CalculateDynamicPrice(int productId, int currentStock)
    {
        var product = _context.Products.Find(productId);
        if (product == null) return 0;

        var multiplier = currentStock < 10 ? 1.5m : currentStock < 50 ? 1.2m : 1.0m;
        return product.Price * multiplier;
    }

    public async Task<decimal> GetBundlePrice(int[] productIds)
    {
        decimal total = 0;
        foreach (var id in productIds)
        {
            var product = await _context.Products.FindAsync(id);
            if (product != null)
            {
                var isAvailable = await _inventoryService.CheckAndReserveStock(id, 0);
                total += isAvailable ? product.Price * 0.9m : product.Price;
            }
        }
        return total;
    }
}
