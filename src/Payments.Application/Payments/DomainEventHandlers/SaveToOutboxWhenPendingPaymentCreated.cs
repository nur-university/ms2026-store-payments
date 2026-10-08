using Joseco.DDD.Core.Abstractions;
using Joseco.Outbox.Contracts.Model;
using Joseco.Outbox.Contracts.Service;
using MediatR;
using Payments.Domain.Payments.Events;

namespace Payments.Application.Payments.DomainEventHandlers;

internal class SaveToOutboxWhenPendingPaymentCreated : INotificationHandler<PendingPaymentCreated>
{
    private readonly IOutboxService<DomainEvent> _outboxDatabase;

    public SaveToOutboxWhenPendingPaymentCreated(IOutboxService<DomainEvent> outboxDatabase)
    {
        _outboxDatabase = outboxDatabase;
    }

    public async Task Handle(PendingPaymentCreated notification, CancellationToken cancellationToken)
    {
        if(notification.SourceType != "orders")
        {
            return;
        }
        OutboxMessage<DomainEvent> outboxMessage = new(notification);

        await _outboxDatabase.AddAsync(outboxMessage);
    }
}
