using Joseco.DDD.Core.Abstractions;

namespace Payments.Domain.Payments.Events;

public record PaymentCompleted(Guid PaymentId, Guid? SourceId, string? SourceType, string? InvoiceNumber = null, string? Method = null) : DomainEvent;
