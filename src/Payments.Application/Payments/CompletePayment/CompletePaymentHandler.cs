using Joseco.DDD.Core.Abstractions;
using Joseco.DDD.Core.Results;
using MediatR;
using Payments.Domain.Payments;

namespace Payments.Application.Payments.CompletePayment;

internal class CompletePaymentHandler(IPaymentRepository paymentRepository, IUnitOfWork unitOfWork)
    : IRequestHandler<CompletePaymentCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CompletePaymentCommand request, CancellationToken cancellationToken)
    {
        Payment? payment = await paymentRepository.GetBySourceAsync(request.SourceId, request.SourceType);
        if (payment == null)
        {
            return Result.Failure<Guid>(PaymentErrors.NotFound(request.SourceId, request.SourceType));
        }

        // Already completed: idempotent success, no second PaymentCompleted event.
        if (payment.Status == PaymentStatus.Completed)
        {
            return payment.Id;
        }

        if (payment.Status != PaymentStatus.Pending)
        {
            return Result.Failure<Guid>(PaymentErrors.NotPending(payment.Status));
        }

        payment.MarkAsPaid();
        await unitOfWork.CommitAsync(cancellationToken);

        return payment.Id;
    }
}
