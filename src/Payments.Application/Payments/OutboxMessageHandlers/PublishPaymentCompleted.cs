using Joseco.Communication.External.Contracts.Services;
using Joseco.Outbox.Contracts.Model;
using MediatR;
using Payments.Domain.Payments.Events;

namespace Payments.Application.Payments.OutboxMessageHandlers;

internal class PublishPaymentCompleted : INotificationHandler<OutboxMessage<PaymentCompleted>>
{
    private readonly IExternalPublisher _externalPublisher;

    public PublishPaymentCompleted(IExternalPublisher externalPublisher)
    {
        _externalPublisher = externalPublisher;
    }

    public async Task Handle(OutboxMessage<PaymentCompleted> notification, CancellationToken cancellationToken)
    {
        PaymentCompleted domainEvent = notification.Content;

        if (domainEvent.SourceType == "orders" && domainEvent.SourceId.HasValue && domainEvent.SourceId != Guid.Empty)
        {
            await _externalPublisher.PublishAsync(
                new Nur.Store2026.Integration.Payments.PaymentCompleted(domainEvent.SourceId.Value)
                {
                    Id = domainEvent.Id,
                    InvoiceNumber = domainEvent.InvoiceNumber,
                    PaymentMethod = domainEvent.Method
                });
        }
    }
}
