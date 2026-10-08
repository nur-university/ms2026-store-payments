using Joseco.DDD.Core.Results;
using MediatR;

namespace Payments.Application.Payments.CompletePayment;

public record CompletePaymentCommand(Guid SourceId, string SourceType) : IRequest<Result<Guid>>;
