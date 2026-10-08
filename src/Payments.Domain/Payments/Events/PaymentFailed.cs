using Joseco.DDD.Core.Abstractions;

namespace Payments.Domain.Payments.Events;

public record PaymentFailed(Guid PaymentId, Guid? SourceId, string? SourceType, string Reason) : DomainEvent;
