using Joseco.DDD.Core.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Nur.Store2026.Integration.Orders;
using Payments.Application.Payments.CancelPayment;
using Payments.Domain.Payments;

namespace Payments.Test.Application;

public class CancelPaymentTests
{
    private readonly Mock<IPaymentRepository> _payments = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Guid _orderId = Guid.NewGuid();

    private CancelPaymentWhenOrderPaymentCancellationRequested Consumer() =>
        new(_payments.Object, _unitOfWork.Object, NullLogger<CancelPaymentWhenOrderPaymentCancellationRequested>.Instance);

    private Payment PaymentFor(Action<Payment>? arrange = null)
    {
        Payment payment = new(100m, _orderId, "orders");
        arrange?.Invoke(payment);
        payment.ClearDomainEvents();
        _payments.Setup(r => r.GetBySourceAsync(_orderId, "orders")).ReturnsAsync(payment);
        return payment;
    }

    [Fact]
    public async Task CancelsThePendingPayment()
    {
        Payment payment = PaymentFor();

        await Consumer().HandleAsync(new OrderPaymentCancellationRequested(_orderId, "canceled"), CancellationToken.None);

        Assert.Equal(PaymentStatus.Cancelled, payment.Status);
        _unitOfWork.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task LeavesAlreadyCompletedOrCanceledPaymentsAlone()
    {
        Payment completed = PaymentFor(p => p.PayInCash());

        await Consumer().HandleAsync(new OrderPaymentCancellationRequested(_orderId, "late"), CancellationToken.None);

        Assert.Equal(PaymentStatus.Completed, completed.Status);
        _unitOfWork.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task WhenThePaymentDoesNotExistYet_DoesNothing()
    {
        _payments.Setup(r => r.GetBySourceAsync(It.IsAny<Guid>(), It.IsAny<string>())).ReturnsAsync((Payment?)null);

        await Consumer().HandleAsync(new OrderPaymentCancellationRequested(_orderId, "canceled"), CancellationToken.None);

        _unitOfWork.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
