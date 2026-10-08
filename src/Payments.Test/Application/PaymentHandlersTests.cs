using Joseco.DDD.Core.Abstractions;
using Joseco.DDD.Core.Results;
using Moq;
using Payments.Application.Payments.CompletePayment;
using Payments.Application.Payments.FailPayment;
using Payments.Domain.Payments;

namespace Payments.Test.Application;

public class PaymentHandlersTests
{
    private readonly Mock<IPaymentRepository> _payments = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Guid _orderId = Guid.NewGuid();

    private Payment PendingPayment()
    {
        Payment payment = new(100m, _orderId, "orders");
        payment.ClearDomainEvents();
        _payments.Setup(r => r.GetBySourceAsync(_orderId, "orders")).ReturnsAsync(payment);
        return payment;
    }

    private void VerifySaved(Times times) =>
        _unitOfWork.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), times);

    [Fact]
    public async Task Complete_PendingPayment_CompletesAndSaves()
    {
        Payment payment = PendingPayment();
        CompletePaymentHandler handler = new(_payments.Object, _unitOfWork.Object);

        Result<Guid> result = await handler.Handle(new CompletePaymentCommand(_orderId, "orders"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(PaymentStatus.Completed, payment.Status);
        VerifySaved(Times.Once());
    }

    [Fact]
    public async Task Complete_AlreadyCompleted_IsIdempotent()
    {
        Payment payment = PendingPayment();
        payment.MarkAsPaid();
        payment.ClearDomainEvents();
        CompletePaymentHandler handler = new(_payments.Object, _unitOfWork.Object);

        Result<Guid> result = await handler.Handle(new CompletePaymentCommand(_orderId, "orders"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(payment.DomainEvents);
        VerifySaved(Times.Never());
    }

    [Fact]
    public async Task Complete_UnknownPayment_ReturnsNotFound()
    {
        _payments.Setup(r => r.GetBySourceAsync(It.IsAny<Guid>(), It.IsAny<string>())).ReturnsAsync((Payment?)null);
        CompletePaymentHandler handler = new(_payments.Object, _unitOfWork.Object);

        Result<Guid> result = await handler.Handle(new CompletePaymentCommand(_orderId, "orders"), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
        VerifySaved(Times.Never());
    }

    [Fact]
    public async Task Fail_PendingPayment_FailsAndSaves()
    {
        Payment payment = PendingPayment();
        FailPaymentHandler handler = new(_payments.Object, _unitOfWork.Object);

        Result<Guid> result = await handler.Handle(new FailPaymentCommand(_orderId, "orders", "declined"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(PaymentStatus.Failed, payment.Status);
        VerifySaved(Times.Once());
    }

    [Fact]
    public async Task Fail_AlreadyFailed_IsIdempotent()
    {
        Payment payment = PendingPayment();
        payment.MarkAsFailed("first");
        payment.ClearDomainEvents();
        FailPaymentHandler handler = new(_payments.Object, _unitOfWork.Object);

        Result<Guid> result = await handler.Handle(new FailPaymentCommand(_orderId, "orders", "again"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(payment.DomainEvents);
        VerifySaved(Times.Never());
    }
}
