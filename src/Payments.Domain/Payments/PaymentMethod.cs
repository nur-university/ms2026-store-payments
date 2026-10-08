namespace Payments.Domain.Payments;

/// <summary>How the customer paid at the cashier.</summary>
public enum PaymentMethod
{
    /// <summary>Cash handed to the cashier; the change is computed.</summary>
    Cash,

    /// <summary>Bank transfer; the reference is the transfer number.</summary>
    BankTransfer,

    /// <summary>Card paid on a POS terminal; the reference is the voucher or authorization code.</summary>
    Card
}
