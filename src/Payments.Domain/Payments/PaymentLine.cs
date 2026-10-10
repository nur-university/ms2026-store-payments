using Joseco.DDD.Core.Abstractions;

namespace Payments.Domain.Payments;

/// <summary>
/// One product of the sale this charge order collects, as it was when the sale was confirmed (the "detalle" of the
/// orden de cobro and of the invoice).
/// </summary>
public class PaymentLine : Entity
{
    public Guid PaymentId { get; private set; }
    public Guid ProductId { get; private set; }
    public string ProductName { get; private set; } = string.Empty;
    public int Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }

    public decimal Subtotal => UnitPrice * Quantity;

    public PaymentLine(Guid paymentId, Guid productId, string productName, int quantity, decimal unitPrice)
    {
        PaymentId = paymentId;
        ProductId = productId;
        ProductName = productName;
        Quantity = quantity;
        UnitPrice = unitPrice;
    }

    // EF Core
    private PaymentLine() { }
}
