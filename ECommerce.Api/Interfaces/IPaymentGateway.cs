namespace ECommerce.Api.Interfaces;

public interface IPaymentGateway
{
    Task<PaymentGatewayResult> ChargeAsync(decimal amount, string currency, string cardToken);
    Task<PaymentGatewayResult> RefundAsync(string transactionId, decimal amount);
}

public class PaymentGatewayResult
{
    public bool Success { get; set; }
    public string TransactionId { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
}
