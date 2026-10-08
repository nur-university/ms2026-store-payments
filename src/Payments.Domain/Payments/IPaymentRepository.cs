using Joseco.DDD.Core.Abstractions;

namespace Payments.Domain.Payments;

public interface IPaymentRepository : IRepository<Payment>
{
    Task<Payment?> GetBySourceAsync(Guid sourceId, string sourceType);

    /// <summary>The charge orders still waiting for the customer to pay, oldest first (the cashier's queue).</summary>
    Task<(IReadOnlyList<Payment> Payments, int Total)> GetPendingAsync(int skip, int take);
}
