using Joseco.DDD.Core.Abstractions;
using Payments.Domain.Payments.Events;

namespace Payments.Domain.Payments;

public class Payment : AggregateRoot
{
    public DateTime CreatedOn { get; set; }
    public DateTime? PaidOn { get; set; }
    public PaymentStatus Status { get; private set; }

    public Guid? SourceId { get; set; }
    public string? SourceType { get; set; }

    public decimal Amount { get; set; }

    public Payment(decimal amount, Guid? sourceId, string? sourceType) : base(
        Guid.NewGuid())
    {
        Amount = amount;
        CreatedOn = DateTime.UtcNow;
        Status = PaymentStatus.Pending;
        SourceId = sourceId;
        SourceType = sourceType;

        AddDomainEvent(new PendingPaymentCreated(Id, SourceId, SourceType));
    }

    public void MarkAsPaid()
    {
        if (Status != PaymentStatus.Pending)
            throw new InvalidOperationException("Payment can only be marked as paid if it is pending.");
        Status = PaymentStatus.Completed;
        PaidOn = DateTime.UtcNow;
    }

    private Payment() { } // For EF Core
}
