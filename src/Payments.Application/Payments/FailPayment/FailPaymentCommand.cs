using Joseco.DDD.Core.Results;
using MediatR;

namespace Payments.Application.Payments.FailPayment;

public record FailPaymentCommand(Guid SourceId, string SourceType, string Reason) : IRequest<Result<Guid>>;
