using Joseco.Communication.External.Contracts.Services;
using Joseco.Outbox.Contracts.Model;
using MediatR;
using Payments.Domain.Payments.Events;

namespace Payments.Application.Payments.OutboxMessageHandlers;

internal class PublishPaymentFailed : INotificationHandler<OutboxMessage<PaymentFailed>>
{
    private readonly IExternalPublisher _externalPublisher;

    public PublishPaymentFailed(IExternalPublisher externalPublisher)
    {
        _externalPublisher = externalPublisher;
    }

    public async Task Handle(OutboxMessage<PaymentFailed> notification, CancellationToken cancellationToken)
    {
        PaymentFailed domainEvent = notification.Content;

        if (domainEvent.SourceType == "orders" && domainEvent.SourceId.HasValue && domainEvent.SourceId != Guid.Empty)
        {
            await _externalPublisher.PublishAsync(
                new Nur.Store2026.Integration.Payments.PaymentFailed(domainEvent.SourceId.Value, domainEvent.Reason) { Id = domainEvent.Id });
        }
    }
}
