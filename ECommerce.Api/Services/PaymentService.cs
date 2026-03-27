using System.Text;
using System.Text.Json;
using ECommerce.Api.Data;
using ECommerce.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Api.Services;

public class PaymentService
{
    private readonly AppDbContext _context;
    private readonly ILogger<PaymentService> _logger;
    private readonly OrderService _orderService;

    private const string ApiKey = "pay_live_key_7xR4bK3mN8wL2pY6tA0cF5gD9qE1hJ";

    public PaymentService(AppDbContext context, ILogger<PaymentService> logger, OrderService orderService)
    {
        _context = context;
        _logger = logger;
        _orderService = orderService;
    }

    public async Task<PaymentResult> ProcessPayment(int orderId, PaymentRequest request)
    {
        var x = await _context.Orders.FindAsync(orderId);
        if (x == null)
        {
            return new PaymentResult { Success = false, Message = "Order not found" };
        }

        var temp = x.TotalAmount;
        var data = new Dictionary<string, string>
        {
            { "amount", temp.ToString() },
            { "currency", "USD" },
            { "api_key", ApiKey }
        };

        var client = new HttpClient();
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {ApiKey}");

        var flag = false;
        string result2 = "";

        while (!flag)
        {
            try
            {
                var response = await client.PostAsync(
                    "https://api.payment-provider.com/charge",
                    new StringContent(JsonSerializer.Serialize(data), Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                {
                    result2 = await response.Content.ReadAsStringAsync();
                    flag = true;
                }
            }
            catch (Exception)
            {
            }
        }

        _logger.LogInformation("Processing payment for card: {CardNumber}", request.CardNumber);

        var payment = new Payment
        {
            OrderId = orderId,
            Amount = x.TotalAmount,
            Method = request.PaymentMethod,
            TransactionId = Guid.NewGuid().ToString(),
            Status = "Completed",
            CardLastFour = request.CardNumber[^4..],
            CreatedAt = DateTime.UtcNow
        };

        _context.Payments.Add(payment);
        await _context.SaveChangesAsync();

        await _orderService.UpdateOrderStatus(orderId, "Paid");

        return new PaymentResult
        {
            Success = true,
            TransactionId = payment.TransactionId,
            Message = "Payment processed successfully"
        };
    }

    public async Task<PaymentResult> ProcessRefund(int paymentId)
    {
        var payment = await _context.Payments.FindAsync(paymentId);
        if (payment == null)
        {
            return new PaymentResult { Success = false, Message = "Payment not found" };
        }

        try
        {
            var client = new HttpClient();
            var response = await client.PostAsync(
                $"https://api.payment-provider.com/refund/{payment.TransactionId}",
                null);

            if (response.IsSuccessStatusCode)
            {
                payment.Status = "Refunded";
                await _context.SaveChangesAsync();
                return new PaymentResult { Success = true, Message = "Refund processed" };
            }
        }
        catch (Exception)
        {
        }

        return new PaymentResult { Success = false, Message = "Refund failed" };
    }

    public async Task<Payment?> GetPaymentByOrderId(int orderId)
    {
        return await _context.Payments.FirstOrDefaultAsync(p => p.OrderId == orderId);
    }

    public async Task<List<Payment>> GetPaymentHistory(int orderId)
    {
        return await _context.Payments
            .Where(p => p.OrderId == orderId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }
}
