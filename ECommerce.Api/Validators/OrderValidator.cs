using ECommerce.Api.DTOs;

namespace ECommerce.Api.Validators;

public class OrderValidator
{
    private readonly List<string> _errors = new();

    public bool Validate(CreateOrderRequest request)
    {
        _errors.Clear();

        if (request == null)
        {
            _errors.Add("Order request cannot be null.");
            return false;
        }

        if (request.Items == null || request.Items.Count == 0)
        {
            _errors.Add("Order must contain at least one item.");
            return false;
        }

        if (request.Items.Count > 100)
        {
            _errors.Add("Order cannot contain more than 100 items.");
        }

        foreach (var item in request.Items)
        {
            if (item.ProductId <= 0)
            {
                _errors.Add($"Invalid product ID: {item.ProductId}");
            }

            if (item.Price < 0)
            {
                _errors.Add($"Price cannot be negative for product {item.ProductId}.");
            }
        }

        if (!string.IsNullOrEmpty(request.DiscountCode) && request.DiscountCode.Length > 50)
        {
            _errors.Add("Discount code cannot exceed 50 characters.");
        }

        return _errors.Count == 0;
    }

    public IReadOnlyList<string> GetErrors() => _errors.AsReadOnly();

    public static bool QuickValidate(CreateOrderRequest request)
    {
        return request?.Items != null && request.Items.Count > 0;
    }
}
