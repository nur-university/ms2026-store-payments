using Joseco.DDD.Core.Results;

namespace Payments.Domain.Payments;

public static class PaymentErrors
{
    public static Error NotPending(PaymentStatus status) =>
        new("Payment.NotPending", $"The payment is {status}, so it can no longer be collected.", ErrorType.Conflict);

    public static Error NotFound(Guid sourceId, string sourceType) =>
        new("Payment.NotFound", $"No payment found for {sourceType} {sourceId}.", ErrorType.NotFound);

    public static Error CashReceivedInsufficient(decimal amount, decimal? received) =>
        new("Payment.CashReceivedInsufficient",
            $"The cash received ({received?.ToString() ?? "none"}) does not cover the amount to pay ({amount}).",
            ErrorType.Validation);

    public static Error ReferenceRequired(PaymentMethod method) =>
        new("Payment.ReferenceRequired",
            method == PaymentMethod.BankTransfer
                ? "A bank transfer needs the transfer number as reference."
                : "A card payment needs the POS voucher or authorization code as reference.",
            ErrorType.Validation);

    public static Error UnknownMethod() =>
        new("Payment.UnknownMethod", "The payment method must be Cash, BankTransfer or Card.", ErrorType.Validation);

    public static Error NotInvoiced(Guid sourceId) =>
        new("Payment.NotInvoiced", $"The payment of {sourceId} has not been collected yet, so there is no invoice.", ErrorType.Conflict);
}
