namespace ECommerce.Api.DTOs;

public class CartItemDto
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public double Price { get; set; }
}

public class CreateOrderRequest
{
    public List<CartItemDto> Items { get; set; } = new();
    public string? DiscountCode { get; set; }
}
