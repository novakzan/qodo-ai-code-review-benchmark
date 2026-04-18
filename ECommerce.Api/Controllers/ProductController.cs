using ECommerce.Api.DTOs;
using ECommerce.Api.Models;
using ECommerce.Api.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductController : ControllerBase
{
    private readonly ProductRepository _productRepository;
    private readonly ILogger<ProductController> _logger;

    public ProductController(ProductRepository productRepository, ILogger<ProductController> logger)
    {
        _productRepository = productRepository;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetProducts([FromQuery] ProductSearchDto search)
    {
        List<Product> products;

        if (!string.IsNullOrEmpty(search.SearchTerm))
        {
            products = await _productRepository.SearchProducts(search.SearchTerm);
        }
        else
        {
            products = await _productRepository.GetFilteredProducts(
                search.Category, search.MinPrice, search.MaxPrice,
                search.SortBy, search.Descending);
        }

        return Ok(products);
    }

    [HttpGet("paginated")]
    public async Task<IActionResult> GetPaginatedProducts([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var products = await _productRepository.GetPaginatedProducts(page, pageSize);
        return Ok(products);
    }

    [HttpGet("with-orders")]
    public async Task<IActionResult> GetProductsWithOrderCounts()
    {
        var products = await _productRepository.GetProductsWithOrderCounts();
        return Ok(products);
    }

    [HttpGet("{productId}")]
    public async Task<IActionResult> GetProduct(int productId)
    {
        var product = await _productRepository.GetById(productId);
        if (product == null)
            return NotFound(new { message = "Product not found" });

        return Ok(product);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateProduct([FromBody] Product product)
    {
        var created = await _productRepository.Create(product);
        return CreatedAtAction(nameof(GetProduct), new { productId = created.Id }, created);
    }

    [HttpPut("{productId}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateProduct(int productId, [FromBody] Product product)
    {
        product.Id = productId;
        var result = await _productRepository.Update(product);
        if (!result)
            return NotFound(new { message = "Product not found" });

        return Ok(new { message = "Product updated successfully" });
    }

    [HttpDelete("{productId}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteProduct(int productId)
    {
        var result = await _productRepository.Delete(productId);
        if (!result)
            return NotFound(new { message = "Product not found" });

        return Ok(new { message = "Product deleted successfully" });
    }
}
