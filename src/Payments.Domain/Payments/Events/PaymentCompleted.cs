using Joseco.DDD.Core.Abstractions;

namespace Payments.Domain.Payments.Events;

public record PaymentCompleted(Guid PaymentId, Guid? SourceId, string? SourceType) : DomainEvent;
