using Joseco.DDD.Core.Abstractions;
using Joseco.DDD.Core.Results;
using MediatR;
using Payments.Domain.Payments;

namespace Payments.Application.Payments.CollectPayment;

/// <param name="SourceId">The order whose charge order is being paid.</param>
/// <param name="Reference">Transfer number or POS voucher/authorization code (not for cash).</param>
/// <param name="AmountReceived">Cash handed over (only for cash).</param>
/// <param name="CollectedBy">The cashier (user id from the access token).</param>
public record CollectPaymentCommand(
    Guid SourceId,
    string SourceType,
    PaymentMethod Method,
    string? Reference,
    decimal? AmountReceived,
    Guid CollectedBy) : IRequest<Result<CollectedPayment>>;

public record CollectedPayment(Guid PaymentId, string InvoiceNumber, decimal Amount, decimal ChangeGiven, string Method);

internal class CollectPaymentHandler(
    IPaymentRepository paymentRepository,
    IInvoiceNumberGenerator invoiceNumbers,
    IUnitOfWork unitOfWork) : IRequestHandler<CollectPaymentCommand, Result<CollectedPayment>>
{
    public async Task<Result<CollectedPayment>> Handle(CollectPaymentCommand request, CancellationToken cancellationToken)
    {
        Payment? payment = await paymentRepository.GetBySourceAsync(request.SourceId, request.SourceType);
        if (payment == null)
        {
            return Result.Failure<CollectedPayment>(PaymentErrors.NotFound(request.SourceId, request.SourceType));
        }

        // The cashier pressed the button twice (or retried after a timeout): answer with the invoice already issued
        // instead of charging and invoicing again.
        if (payment.Status == PaymentStatus.Completed && payment.Method == request.Method)
        {
            return ToResult(payment);
        }

        if (payment.Status != PaymentStatus.Pending)
        {
            return Result.Failure<CollectedPayment>(PaymentErrors.NotPending(payment.Status));
        }

        try
        {
            // Validate first: an invoice number is only consumed by a payment that is really going to be collected.
            payment.ValidateCollection(request.Method, request.Reference, request.AmountReceived);
        }
        catch (DomainException ex)
        {
            return Result.Failure<CollectedPayment>(ex.Error);
        }

        string invoiceNumber = await invoiceNumbers.NextAsync(cancellationToken);
        payment.Collect(request.Method, request.Reference, request.AmountReceived, request.CollectedBy, invoiceNumber);

        await unitOfWork.CommitAsync(cancellationToken);

        return ToResult(payment);
    }

    private static CollectedPayment ToResult(Payment payment) =>
        new(payment.Id, payment.InvoiceNumber!, payment.Amount, payment.ChangeGiven ?? 0, payment.Method!.Value.ToString());
}
