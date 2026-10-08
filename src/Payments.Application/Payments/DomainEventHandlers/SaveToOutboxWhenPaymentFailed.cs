using Joseco.DDD.Core.Abstractions;
using Joseco.Outbox.Contracts.Model;
using Joseco.Outbox.Contracts.Service;
using MediatR;
using Payments.Domain.Payments.Events;

namespace Payments.Application.Payments.DomainEventHandlers;

internal class SaveToOutboxWhenPaymentFailed : INotificationHandler<PaymentFailed>
{
    private readonly IOutboxService<DomainEvent> _outboxService;

    public SaveToOutboxWhenPaymentFailed(IOutboxService<DomainEvent> outboxService)
    {
        _outboxService = outboxService;
    }

    public async Task Handle(PaymentFailed notification, CancellationToken cancellationToken)
    {
        if (notification.SourceType != "orders")
        {
            return;
        }
        await _outboxService.AddAsync(new OutboxMessage<DomainEvent>(notification));
    }
}
