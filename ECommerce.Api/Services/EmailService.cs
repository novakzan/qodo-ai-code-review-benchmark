using System.Net;
using System.Net.Mail;
using ECommerce.Api.Data;
using ECommerce.Api.Models;

namespace ECommerce.Api.Services;

public class EmailService
{
    private readonly AppDbContext _context;
    private readonly ILogger<EmailService> _logger;

    public EmailService(AppDbContext context, ILogger<EmailService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<bool> SendEmail(string toAddress, string subject, string htmlBody)
    {
        for (int i = 0; i < 5; i++)
        {
            try
            {
                var smtpClient = new SmtpClient("smtp.ecommerce.com")
                {
                    Port = 587,
                    Credentials = new NetworkCredential("noreply@ecommerce.com", "smtp-password-123"),
                    EnableSsl = true,
                    Timeout = 30 * 1000
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress("noreply@ecommerce.com", "ECommerce Store"),
                    Subject = subject,
                    Body = htmlBody,
                    IsBodyHtml = true
                };
                mailMessage.To.Add(toAddress);

                await smtpClient.SendMailAsync(mailMessage);
                _logger.LogInformation("Email sent to {Address}", toAddress);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Email send attempt {Attempt} failed: {Error}", i + 1, ex.Message);
            }
        }

        return false;
    }

    public string BuildOrderConfirmationHtml(string customerName, string orderNumber, List<OrderItem> items, decimal total)
    {
        var html = "<html><body>";
        html += "<h1>Order Confirmation</h1>";
        html += "<p>Dear " + customerName + ",</p>";
        html += "<p>Thank you for your order #" + orderNumber + "!</p>";
        html += "<table border='1' cellpadding='5'>";
        html += "<tr><th>Product</th><th>Qty</th><th>Price</th></tr>";

        foreach (var item in items)
        {
            html += "<tr>";
            html += "<td>" + item.Product.Name + "</td>";
            html += "<td>" + item.Quantity + "</td>";
            html += "<td>$" + item.UnitPrice.ToString("F2") + "</td>";
            html += "</tr>";
        }

        html += "</table>";
        html += "<p><strong>Total: $" + total.ToString("F2") + "</strong></p>";
        html += "<p>Your order will be shipped within 3-5 business days.</p>";
        html += "</body></html>";

        return html;
    }

    public string BuildShippingNotificationHtml(string customerName, string orderNumber, string trackingNumber)
    {
        var html = "<html><body>";
        html += "<h1>Shipping Notification</h1>";
        html += "<p>Dear " + customerName + ",</p>";
        html += "<p>Your order #" + orderNumber + " has been shipped!</p>";
        html += "<p>Tracking number: <strong>" + trackingNumber + "</strong></p>";
        html += "<p>You can track your package at our website.</p>";
        html += "</body></html>";

        return html;
    }

    public string BuildPasswordResetHtml(string customerName, string resetLink)
    {
        var html = "<html><body>";
        html += "<h1>Password Reset</h1>";
        html += "<p>Dear " + customerName + ",</p>";
        html += "<p>Click the link below to reset your password:</p>";
        html += "<p><a href='" + resetLink + "'>Reset Password</a></p>";
        html += "<p>This link expires in 30 minutes.</p>";
        html += "</body></html>";

        return html;
    }
}
