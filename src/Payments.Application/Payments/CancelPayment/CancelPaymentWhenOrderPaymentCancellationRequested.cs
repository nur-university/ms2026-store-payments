using Joseco.Communication.External.Contracts.Services;
using Joseco.DDD.Core.Abstractions;
using Microsoft.Extensions.Logging;
using Nur.Store2026.Integration.Orders;
using Payments.Domain.Payments;

namespace Payments.Application.Payments.CancelPayment;

/// <summary>
/// Saga compensation: the order was closed (canceled by the customer, failed or timed out),
/// so the payment still waiting for it must not be collectable any more.
/// </summary>
public class CancelPaymentWhenOrderPaymentCancellationRequested(
    IPaymentRepository paymentRepository,
    IUnitOfWork unitOfWork,
    ILogger<CancelPaymentWhenOrderPaymentCancellationRequested> logger)
    : IIntegrationMessageConsumer<OrderPaymentCancellationRequested>
{
    public async Task HandleAsync(OrderPaymentCancellationRequested message, CancellationToken cancellationToken)
    {
        Payment? payment = await paymentRepository.GetBySourceAsync(message.OrderId, "orders");
        if (payment == null)
        {
            // The pending payment is not registered yet; Orders asks again when it learns about it.
            logger.LogInformation("No payment to cancel yet for order {OrderId}", message.OrderId);
            return;
        }

        if (payment.Status != PaymentStatus.Pending)
        {
            return; // already completed, failed or canceled
        }

        payment.Cancel();
        await unitOfWork.CommitAsync(cancellationToken);
    }
}
