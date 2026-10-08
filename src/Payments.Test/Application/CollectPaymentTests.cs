using Joseco.DDD.Core.Abstractions;
using Joseco.DDD.Core.Results;
using Moq;
using Payments.Application.Payments.CollectPayment;
using Payments.Application.Payments.Queries;
using Payments.Domain.Payments;

namespace Payments.Test.Application;

public class CollectPaymentTests
{
    private readonly Mock<IPaymentRepository> _payments = new();
    private readonly Mock<IInvoiceNumberGenerator> _invoices = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Guid _orderId = Guid.NewGuid();
    private readonly Guid _cashier = Guid.NewGuid();

    public CollectPaymentTests()
    {
        _invoices.Setup(i => i.NextAsync(It.IsAny<CancellationToken>())).ReturnsAsync("F-00000042");
    }

    private Payment PendingPayment(decimal amount = 100m)
    {
        Payment payment = new(amount, _orderId, "orders", "Ana Perez", "1234567");
        payment.ClearDomainEvents();
        _payments.Setup(r => r.GetBySourceAsync(_orderId, "orders")).ReturnsAsync(payment);
        return payment;
    }

    private CollectPaymentHandler Handler() => new(_payments.Object, _invoices.Object, _unitOfWork.Object);

    private CollectPaymentCommand Command(PaymentMethod method = PaymentMethod.Cash, string? reference = null, decimal? received = 120m) =>
        new(_orderId, "orders", method, reference, received, _cashier);

    private void VerifySaved(Times times) =>
        _unitOfWork.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), times);

    [Fact]
    public async Task Collect_InCash_ReturnsTheInvoiceAndTheChange()
    {
        Payment payment = PendingPayment(100m);

        Result<CollectedPayment> result = await Handler().Handle(Command(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("F-00000042", result.Value.InvoiceNumber);
        Assert.Equal(20m, result.Value.ChangeGiven);
        Assert.Equal("Cash", result.Value.Method);
        Assert.Equal(PaymentStatus.Completed, payment.Status);
        Assert.Equal(_cashier, payment.CollectedBy);
        VerifySaved(Times.Once());
    }

    [Fact]
    public async Task Collect_ByCard_StoresThePosVoucher()
    {
        Payment payment = PendingPayment();

        Result<CollectedPayment> result = await Handler().Handle(
            Command(PaymentMethod.Card, "POS-AUTH-5521", null), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("POS-AUTH-5521", payment.Reference);
    }

    [Fact]
    public async Task Collect_TheSamePaymentTwice_DoesNotInvoiceAgain()
    {
        PendingPayment();
        await Handler().Handle(Command(), CancellationToken.None);

        Result<CollectedPayment> again = await Handler().Handle(Command(), CancellationToken.None);

        Assert.True(again.IsSuccess);
        Assert.Equal("F-00000042", again.Value.InvoiceNumber);
        _invoices.Verify(i => i.NextAsync(It.IsAny<CancellationToken>()), Times.Once);
        VerifySaved(Times.Once());
    }

    [Fact]
    public async Task Collect_WithAnotherMethodAfterItWasPaid_IsAConflict()
    {
        PendingPayment();
        await Handler().Handle(Command(), CancellationToken.None);

        Result<CollectedPayment> result = await Handler().Handle(
            Command(PaymentMethod.Card, "POS-1", null), CancellationToken.None);

        Assert.Equal(ErrorType.Conflict, result.Error.Type);
    }

    [Fact]
    public async Task Collect_ACanceledPayment_IsAConflict()
    {
        Payment payment = PendingPayment();
        payment.Cancel();

        Result<CollectedPayment> result = await Handler().Handle(Command(), CancellationToken.None);

        Assert.Equal(ErrorType.Conflict, result.Error.Type);
        Assert.Equal(PaymentStatus.Cancelled, payment.Status);
        VerifySaved(Times.Never());
    }

    [Fact]
    public async Task Collect_WithInvalidData_ReportsTheDomainErrorAndSavesNothing()
    {
        PendingPayment(100m);

        Result<CollectedPayment> notEnoughCash = await Handler().Handle(Command(received: 50m), CancellationToken.None);
        Result<CollectedPayment> noReference = await Handler().Handle(Command(PaymentMethod.BankTransfer, null, null), CancellationToken.None);

        Assert.Equal("Payment.CashReceivedInsufficient", notEnoughCash.Error.Code);
        Assert.Equal("Payment.ReferenceRequired", noReference.Error.Code);
        VerifySaved(Times.Never());
    }

    [Fact]
    public async Task Collect_UnknownOrder_IsNotFound()
    {
        _payments.Setup(r => r.GetBySourceAsync(It.IsAny<Guid>(), It.IsAny<string>())).ReturnsAsync((Payment?)null);

        Result<CollectedPayment> result = await Handler().Handle(Command(), CancellationToken.None);

        Assert.Equal(ErrorType.NotFound, result.Error.Type);
    }

    // ---- invoice

    [Fact]
    public async Task Invoice_IsAvailableOnlyOnceTheSaleWasCollected()
    {
        Payment payment = PendingPayment(100m);
        GetInvoiceByOrderHandler handler = new(_payments.Object);

        Result<InvoiceDto> beforePaying = await handler.Handle(new GetInvoiceByOrderQuery(_orderId), CancellationToken.None);
        payment.Collect(PaymentMethod.Cash, null, 100m, _cashier, "F-00000042");
        Result<InvoiceDto> afterPaying = await handler.Handle(new GetInvoiceByOrderQuery(_orderId), CancellationToken.None);

        Assert.Equal(ErrorType.Conflict, beforePaying.Error.Type);
        Assert.Equal("F-00000042", afterPaying.Value.InvoiceNumber);
        Assert.Equal("Ana Perez", afterPaying.Value.CustomerName);
        Assert.Equal(100m, afterPaying.Value.Total);
    }

    [Fact]
    public async Task PendingQueue_IsPaged()
    {
        _payments.Setup(r => r.GetPendingAsync(It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync(([], 0));

        await new GetPendingPaymentsHandler(_payments.Object).Handle(new GetPendingPaymentsQuery(3, 10), CancellationToken.None);
        await new GetPendingPaymentsHandler(_payments.Object).Handle(new GetPendingPaymentsQuery(1, 5000), CancellationToken.None);

        _payments.Verify(r => r.GetPendingAsync(20, 10), Times.Once);
        _payments.Verify(r => r.GetPendingAsync(0, 100), Times.Once);
    }
}
