using Payments.Domain.Payments;

namespace Payments.Test;

internal static class PaymentTestExtensions
{
    /// <summary>Collects the exact amount in cash, which is the simplest valid way to complete a payment in a test.</summary>
    public static void PayInCash(this Payment payment) =>
        payment.Collect(PaymentMethod.Cash, null, payment.Amount, Guid.NewGuid(), "F-00000001");
}
