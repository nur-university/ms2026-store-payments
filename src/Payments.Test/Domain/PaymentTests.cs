using Joseco.DDD.Core.Results;
using Payments.Domain.Payments;
using Payments.Domain.Payments.Events;

namespace Payments.Test.Domain;

public class PaymentTests
{
    private static readonly Guid Cashier = Guid.NewGuid();

    private static Payment NewPayment(decimal amount = 100m)
    {
        Payment payment = new(amount, Guid.NewGuid(), "orders", "Ana Perez", "1234567");
        payment.ClearDomainEvents();
        return payment;
    }

    [Fact]
    public void New_IsPendingAndRaisesPendingPaymentCreated()
    {
        Payment payment = new(100m, Guid.NewGuid(), "orders", "Ana Perez", "1234567");

        Assert.Equal(PaymentStatus.Pending, payment.Status);
        Assert.Equal("Ana Perez", payment.CustomerName);
        Assert.Single(payment.DomainEvents.OfType<PendingPaymentCreated>());
    }

    [Fact]
    public void Collect_InCash_ComputesTheChange_AndIssuesTheInvoice()
    {
        Payment payment = NewPayment(100m);

        payment.Collect(PaymentMethod.Cash, null, 150m, Cashier, "F-00000007");

        Assert.Equal(PaymentStatus.Completed, payment.Status);
        Assert.Equal(PaymentMethod.Cash, payment.Method);
        Assert.Equal(150m, payment.AmountReceived);
        Assert.Equal(50m, payment.ChangeGiven);
        Assert.Equal(Cashier, payment.CollectedBy);
        Assert.Equal("F-00000007", payment.InvoiceNumber);
        Assert.NotNull(payment.PaidOn);

        PaymentCompleted completed = Assert.Single(payment.DomainEvents.OfType<PaymentCompleted>());
        Assert.Equal(payment.SourceId, completed.SourceId);
        Assert.Equal("F-00000007", completed.InvoiceNumber);
        Assert.Equal("Cash", completed.Method);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(99.99)]
    public void Collect_InCash_WithoutEnoughMoney_IsRejected(double? received)
    {
        Payment payment = NewPayment(100m);

        DomainException ex = Assert.Throws<DomainException>(() =>
            payment.Collect(PaymentMethod.Cash, null, received.HasValue ? (decimal)received.Value : null, Cashier, "F-1"));

        Assert.Equal("Payment.CashReceivedInsufficient", ex.Error.Code);
        Assert.Equal(PaymentStatus.Pending, payment.Status);
        Assert.Empty(payment.DomainEvents);
    }

    [Theory]
    [InlineData(PaymentMethod.BankTransfer)]
    [InlineData(PaymentMethod.Card)]
    public void Collect_ByTransferOrCard_NeedsAReference(PaymentMethod method)
    {
        Payment payment = NewPayment();

        Assert.Throws<DomainException>(() => payment.Collect(method, "  ", null, Cashier, "F-1"));

        payment.Collect(method, " TRX-998877 ", null, Cashier, "F-1");

        Assert.Equal("TRX-998877", payment.Reference);
        Assert.Equal(payment.Amount, payment.AmountReceived);
        Assert.Equal(0m, payment.ChangeGiven);
        Assert.Equal(PaymentStatus.Completed, payment.Status);
    }

    [Fact]
    public void Collect_Twice_OrAfterCancel_Throws()
    {
        Payment collected = NewPayment();
        collected.PayInCash();
        Payment canceled = NewPayment();
        canceled.Cancel();

        Assert.Throws<InvalidOperationException>(() => collected.PayInCash());
        Assert.Throws<InvalidOperationException>(() => canceled.PayInCash());
    }

    [Fact]
    public void Cancel_WithdrawsAPendingPayment_AndItCanNoLongerBeCollected()
    {
        Payment payment = NewPayment();

        payment.Cancel();

        Assert.Equal(PaymentStatus.Cancelled, payment.Status);
        Assert.Throws<InvalidOperationException>(() => payment.PayInCash());
        Assert.Throws<InvalidOperationException>(() => payment.Cancel());
    }
}
