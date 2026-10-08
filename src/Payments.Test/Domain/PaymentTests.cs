using Payments.Domain.Payments;
using Payments.Domain.Payments.Events;

namespace Payments.Test.Domain;

public class PaymentTests
{
    private static Payment NewPayment()
    {
        Payment payment = new(100m, Guid.NewGuid(), "orders");
        payment.ClearDomainEvents();
        return payment;
    }

    [Fact]
    public void New_IsPendingAndRaisesPendingPaymentCreated()
    {
        Payment payment = new(100m, Guid.NewGuid(), "orders");

        Assert.Equal(PaymentStatus.Pending, payment.Status);
        Assert.Single(payment.DomainEvents.OfType<PendingPaymentCreated>());
    }

    [Fact]
    public void MarkAsPaid_RaisesPaymentCompletedWithSource()
    {
        Payment payment = NewPayment();

        payment.MarkAsPaid();

        Assert.Equal(PaymentStatus.Completed, payment.Status);
        Assert.NotNull(payment.PaidOn);
        PaymentCompleted completed = Assert.Single(payment.DomainEvents.OfType<PaymentCompleted>());
        Assert.Equal(payment.SourceId, completed.SourceId);
        Assert.Equal("orders", completed.SourceType);
    }

    [Fact]
    public void MarkAsFailed_RaisesPaymentFailedWithReason()
    {
        Payment payment = NewPayment();

        payment.MarkAsFailed("insufficient funds");

        Assert.Equal(PaymentStatus.Failed, payment.Status);
        PaymentFailed failed = Assert.Single(payment.DomainEvents.OfType<PaymentFailed>());
        Assert.Equal("insufficient funds", failed.Reason);
    }

    [Fact]
    public void MarkAsPaid_OrMarkAsFailed_WhenNotPending_Throws()
    {
        Payment payment = NewPayment();
        payment.MarkAsPaid();

        Assert.Throws<InvalidOperationException>(() => payment.MarkAsPaid());
        Assert.Throws<InvalidOperationException>(() => payment.MarkAsFailed("late"));
    }
}
