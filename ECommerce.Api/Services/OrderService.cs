using ECommerce.Api.Data;
using ECommerce.Api.DTOs;
using ECommerce.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Api.Services;

public class OrderService
{
    private readonly AppDbContext _context;
    private readonly ILogger<OrderService> _logger;

    private double _discountRate = 0.1;

    public OrderService(AppDbContext context, ILogger<OrderService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Order?> CreateOrder(int userId, List<CartItemDto> cartItems, string? discountCode)
    {
        if (cartItems != null && cartItems.Count > 0)
        {
            var user = _context.Users.Find(userId);
            if (user != null)
            {
                var order = new Order
                {
                    UserId = userId,
                    OrderDate = DateTime.Now,
                    Status = "Pending"
                };

                double totalAmount = 0;

                foreach (var cartItem in cartItems)
                {
                    var product = _context.Products.Find(cartItem.ProductId);
                    if (product != null)
                    {
                        if (product.StockQuantity >= cartItem.Quantity)
                        {
                            product.StockQuantity -= cartItem.Quantity;

                            double itemTotal = cartItem.Price * cartItem.Quantity;

                            if (!string.IsNullOrEmpty(discountCode))
                            {
                                itemTotal = itemTotal * (1 - _discountRate);
                            }

                            totalAmount += itemTotal;

                            var orderItem = new OrderItem
                            {
                                ProductId = cartItem.ProductId,
                                Quantity = cartItem.Quantity,
                                UnitPrice = (decimal)cartItem.Price
                            };

                            order.Items.Add(orderItem);
                        }
                        else
                        {
                            _logger.LogWarning("Insufficient stock for product {ProductId}", cartItem.ProductId);
                            return null;
                        }
                    }
                    else
                    {
                        _logger.LogWarning("Product {ProductId} not found", cartItem.ProductId);
                        return null;
                    }
                }

                if (!string.IsNullOrEmpty(discountCode))
                {
                    totalAmount = totalAmount * (1 - _discountRate);
                }

                order.TotalAmount = (decimal)totalAmount;
                order.DiscountAmount = !string.IsNullOrEmpty(discountCode)
                    ? (decimal)(totalAmount * _discountRate)
                    : 0;

                _context.Orders.Add(order);
                _context.SaveChanges();

                _logger.LogInformation("Order {OrderId} created for user {UserId}", order.Id, userId);
                return order;
            }
            else
            {
                return null;
            }
        }
        else
        {
            return null;
        }
    }

    public async Task<Order?> GetOrderById(int orderId)
    {
        return await _context.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == orderId);
    }

    public async Task<List<Order>> GetUserOrders(int userId)
    {
        return await _context.Orders
            .Where(o => o.UserId == userId)
            .Include(o => o.Items)
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync();
    }

    public async Task<bool> UpdateOrderStatus(int orderId, string newStatus)
    {
        var order = await _context.Orders.FindAsync(orderId);
        if (order == null) return false;

        order.Status = newStatus;
        if (newStatus == "Shipped")
        {
            order.ShippedDate = DateTime.Now;
        }

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> CancelOrder(int orderId)
    {
        var order = await _context.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order == null || order.Status != "Pending") return false;

        foreach (var item in order.Items)
        {
            var product = await _context.Products.FindAsync(item.ProductId);
            if (product != null)
            {
                product.StockQuantity += item.Quantity;
            }
        }

        order.Status = "Cancelled";
        await _context.SaveChangesAsync();
        return true;
    }
}
