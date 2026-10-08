using Joseco.DDD.Core.Abstractions;
using Joseco.DDD.Core.Results;
using MediatR;
using Payments.Domain.Payments;

namespace Payments.Application.Payments.FailPayment;

internal class FailPaymentHandler(IPaymentRepository paymentRepository, IUnitOfWork unitOfWork)
    : IRequestHandler<FailPaymentCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(FailPaymentCommand request, CancellationToken cancellationToken)
    {
        Payment? payment = await paymentRepository.GetBySourceAsync(request.SourceId, request.SourceType);
        if (payment == null)
        {
            return Result.Failure<Guid>(PaymentErrors.NotFound(request.SourceId, request.SourceType));
        }

        if (payment.Status == PaymentStatus.Failed)
        {
            return payment.Id;
        }

        if (payment.Status != PaymentStatus.Pending)
        {
            return Result.Failure<Guid>(PaymentErrors.NotPending(payment.Status));
        }

        payment.MarkAsFailed(request.Reason);
        await unitOfWork.CommitAsync(cancellationToken);

        return payment.Id;
    }
}
