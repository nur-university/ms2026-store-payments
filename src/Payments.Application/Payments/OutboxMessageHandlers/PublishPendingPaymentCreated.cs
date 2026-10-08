using Joseco.Communication.External.Contracts.Services;
using Joseco.Outbox.Contracts.Model;
using MediatR;
using Payments.Domain.Payments.Events;
using Nur.Store2026.Integration.Payments;

namespace Payments.Application.Payments.OutboxMessageHandlers;

internal class PublishPendingPaymentCreated : INotificationHandler<OutboxMessage<PendingPaymentCreated>>
{
    private readonly IExternalPublisher _externalPublisher;

    public PublishPendingPaymentCreated(IExternalPublisher externalPublisher)
    {
        _externalPublisher = externalPublisher;
    }

    public async Task Handle(OutboxMessage<PendingPaymentCreated> notification, CancellationToken cancellationToken)
    {
        PendingPaymentCreated domainEvent = notification.Content;

        if(domainEvent.SourceType == "orders" && domainEvent.SourceId != null && domainEvent.SourceId != Guid.Empty)
        {
            OrderPaymentInProcessRegistered message = new (domainEvent.SourceId.Value) { Id = domainEvent.Id };
            await _externalPublisher.PublishAsync(message);
        }

    }
}
