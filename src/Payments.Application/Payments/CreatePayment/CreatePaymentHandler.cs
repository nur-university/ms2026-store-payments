using Joseco.Communication.External.Contracts.Services;
using Joseco.DDD.Core.Abstractions;
using Joseco.DDD.Core.Results;
using MediatR;
using Nur.Store2026.Integration.Orders;
using Payments.Domain.Payments;

namespace Payments.Application.Payments.CreatePayment;

public class CreatePaymentHandler(IPaymentRepository paymentRepository, IUnitOfWork unitOfWork) : 
    IRequestHandler<CreatePaymentCommand, Result<Guid>>,
    IIntegrationMessageConsumer<OrderReserved>
{
    public async Task<Result<Guid>> Handle(CreatePaymentCommand request, CancellationToken cancellationToken)
    {
        Payment payment = new (request.Amount, request.SourceId, request.SourceType);

        await paymentRepository.AddAsync(payment);
        await unitOfWork.CommitAsync(cancellationToken);

        return Result.Success(payment.Id);
    }

    public async Task HandleAsync(OrderReserved message, CancellationToken cancellationToken)
    {
        // A duplicated OrderReserved (broker redelivery or an outbox row published twice) must not create a second payment.
        if (await paymentRepository.GetBySourceAsync(message.OrderId, "orders") != null)
        {
            return;
        }

        CreatePaymentCommand command = new(message.TotalAmount, message.OrderId, "orders");

        await Handle(command, cancellationToken);
    }
}
