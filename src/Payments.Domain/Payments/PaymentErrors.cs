using Joseco.DDD.Core.Results;

namespace Payments.Domain.Payments;

public static class PaymentErrors
{
    public static Error NotPending(PaymentStatus status) =>
        new("Payment.NotPending", $"The payment is {status}, so it can no longer be completed or failed.", ErrorType.Conflict);

    public static Error NotFound(Guid sourceId, string sourceType) =>
        new("Payment.NotFound", $"No payment found for {sourceType} {sourceId}.", ErrorType.NotFound);
}
