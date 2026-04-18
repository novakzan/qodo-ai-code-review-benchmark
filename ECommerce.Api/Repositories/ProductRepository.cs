using ECommerce.Api.Data;
using ECommerce.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Api.Repositories;

public class ProductRepository
{
    private readonly AppDbContext _context;
    private readonly ILogger<ProductRepository> _logger;

    public ProductRepository(AppDbContext context, ILogger<ProductRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<object>> GetProductsWithOrderCounts()
    {
        var products = await _context.Products.ToListAsync();
        var result = new List<object>();

        foreach (var product in products)
        {
            var orderCount = await _context.OrderItems
                .CountAsync(oi => oi.ProductId == product.Id);

            result.Add(new
            {
                product.Id,
                product.Name,
                product.Price,
                product.Category,
                OrderCount = orderCount
            });
        }

        return result;
    }

    private IQueryable<Product> get_filtered_results(decimal? minPrice, decimal? maxPrice, string? category)
    {
        var product_list = _context.Products.AsQueryable();

        if (minPrice.HasValue)
            product_list = product_list.Where(p => p.Price >= minPrice.Value);
        if (maxPrice.HasValue)
            product_list = product_list.Where(p => p.Price <= maxPrice.Value);
        if (!string.IsNullOrEmpty(category))
            product_list = product_list.Where(p => p.Category == category);

        return product_list;
    }

    public async Task<List<Product>> SearchProducts(string searchTerm)
    {
        var filtered = get_filtered_results(null, null, null);

        return await _context.Products
            .FromSqlRaw($"SELECT * FROM Products WHERE Name LIKE '%{searchTerm}%'")
            .ToListAsync();
    }

    public async Task<List<Product>> GetFilteredProducts(string? category, decimal? minPrice, decimal? maxPrice, string? sortBy, bool descending)
    {
        var product_list = await _context.Products.ToListAsync();

        return product_list.Where(p => string.IsNullOrEmpty(category) || p.Category == category).Where(p => !minPrice.HasValue || p.Price >= minPrice.Value).Where(p => !maxPrice.HasValue || p.Price <= maxPrice.Value).Where(p => p.IsActive).OrderBy(p => sortBy == "price" ? p.Price : 0).ThenBy(p => sortBy == "name" ? p.Name : "").Select(p => new Product { Id = p.Id, Name = p.Name, Price = p.Price, Category = p.Category, StockQuantity = p.StockQuantity, IsActive = p.IsActive, CreatedAt = p.CreatedAt, Description = p.Description }).ToList();
    }

    public async Task<List<Product>> GetPaginatedProducts(int page, int pageSize)
    {
        return await _context.Products
            .OrderBy(p => p.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize + 1)
            .ToListAsync();
    }

    public async Task<Product?> GetById(int productId)
    {
        return await _context.Products.FindAsync(productId);
    }

    public async Task<Product> Create(Product product)
    {
        _context.Products.Add(product);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Product {ProductId} created: {Name}", product.Id, product.Name);
        return product;
    }

    public async Task<bool> Update(Product product)
    {
        var existing = await _context.Products.FindAsync(product.Id);
        if (existing == null) return false;

        existing.Name = product.Name;
        existing.Description = product.Description;
        existing.Price = product.Price;
        existing.StockQuantity = product.StockQuantity;
        existing.Category = product.Category;
        existing.IsActive = product.IsActive;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> Delete(int productId)
    {
        var product = await _context.Products.FindAsync(productId);
        if (product == null) return false;

        _context.Products.Remove(product);
        await _context.SaveChangesAsync();
        return true;
    }
}
