using ECommerce.Api.Data;
using ECommerce.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Api.Services;

public class NotificationService
{
    private readonly AppDbContext _context;
    private readonly EmailService _emailService;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(AppDbContext context, EmailService emailService, ILogger<NotificationService> logger)
    {
        _context = context;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task SendOrderConfirmation(int orderId)
    {
        var order = await _context.Orders
            .Include(o => o.User)
            .Include(o => o.Items)
                .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order == null)
        {
            _logger.LogWarning("Order {OrderId} not found for confirmation email", orderId);
            return;
        }

        var customerName = order.User.FullName;
        var customerEmail = order.User.Email;
        var orderNumber = order.Id.ToString();
        var items = order.Items.ToList();
        var total = order.TotalAmount;
        var discount = order.DiscountAmount;

        var htmlBody = _emailService.BuildOrderConfirmationHtml(customerName, orderNumber, items, total);

        if (discount > 0)
        {
            htmlBody = htmlBody.Replace("</body>", $"<p>Discount applied: -${discount:F2}</p></body>");
        }

        htmlBody = htmlBody.Replace("</body>",
            $"<hr/><p style='font-size:12px'>Order placed on {order.OrderDate:yyyy-MM-dd HH:mm}</p></body>");

        var subject = $"Order Confirmation #{orderNumber}";

        var sent = await _emailService.SendEmail(customerEmail, subject, htmlBody);

        if (sent)
        {
            var notification = new Notification
            {
                UserId = order.UserId,
                Title = $"Order #{orderNumber} confirmed",
                Message = $"Your order for ${total:F2} has been confirmed.",
                Type = "Email",
                CreatedAt = DateTime.UtcNow
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Order confirmation sent for order {OrderId} to {Email}", orderId, customerEmail);
        }
        else
        {
            _logger.LogError("Failed to send order confirmation for order {OrderId}", orderId);
        }
    }

    public async Task SendShippingNotification(int orderId, string trackingNumber)
    {
        var order = await _context.Orders
            .Include(o => o.User)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order == null) return;

        var html = _emailService.BuildShippingNotificationHtml(
            order.User.FullName, order.Id.ToString(), trackingNumber);

        await _emailService.SendEmail(order.User.Email, $"Order #{order.Id} Shipped", html);

        _logger.LogInformation("Shipping notification sent for order {OrderId}", orderId);
    }

    public async Task SendBulkPromotionalEmail(string subject, string htmlContent)
    {
        var users = await _context.Users.ToListAsync();

        foreach (var user in users)
        {
            var personalizedHtml = htmlContent.Replace("{{name}}", user.FullName);
            await _emailService.SendEmail(user.Email, subject, personalizedHtml);
            _logger.LogInformation("Promotional email sent to {Email}", user.Email);
        }
    }

    public async Task<List<Notification>> GetUserNotifications(int userId)
    {
        return await _context.Notifications
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();
    }

    public async Task<bool> MarkAsRead(int notificationId)
    {
        var notification = await _context.Notifications.FindAsync(notificationId);
        if (notification == null) return false;

        notification.IsRead = true;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task SendPasswordResetEmail(int userId, string resetToken)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return;

        var resetLink = $"https://ecommerce.com/reset-password?token={resetToken}&userId={userId}";
        var html = _emailService.BuildPasswordResetHtml(user.FullName, resetLink);

        await _emailService.SendEmail(user.Email, "Password Reset Request", html);
    }
}
