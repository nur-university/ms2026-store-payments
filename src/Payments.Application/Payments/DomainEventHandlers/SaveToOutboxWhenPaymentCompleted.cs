using Joseco.DDD.Core.Abstractions;
using Joseco.Outbox.Contracts.Model;
using Joseco.Outbox.Contracts.Service;
using MediatR;
using Payments.Domain.Payments.Events;

namespace Payments.Application.Payments.DomainEventHandlers;

internal class SaveToOutboxWhenPaymentCompleted : INotificationHandler<PaymentCompleted>
{
    private readonly IOutboxService<DomainEvent> _outboxService;

    public SaveToOutboxWhenPaymentCompleted(IOutboxService<DomainEvent> outboxService)
    {
        _outboxService = outboxService;
    }

    public async Task Handle(PaymentCompleted notification, CancellationToken cancellationToken)
    {
        if (notification.SourceType != "orders")
        {
            return;
        }
        await _outboxService.AddAsync(new OutboxMessage<DomainEvent>(notification));
    }
}
