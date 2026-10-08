namespace Payments.Domain.Payments;

/// <summary>Hands out invoice numbers that are unique and consecutive (F-00000001, F-00000002, ...).</summary>
public interface IInvoiceNumberGenerator
{
    Task<string> NextAsync(CancellationToken cancellationToken = default);
}
