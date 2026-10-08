using Microsoft.EntityFrameworkCore;
using Payments.Domain.Payments;
using Payments.Infrastructure.Persistence.DomainModel;

namespace Payments.Infrastructure.Persistence.Repositories;

/// <summary>Invoice numbers come from a PostgreSQL sequence (created by a migration), so they never repeat across instances.</summary>
internal class InvoiceNumberGenerator(DomainDbContext context) : IInvoiceNumberGenerator
{
    public async Task<string> NextAsync(CancellationToken cancellationToken = default)
    {
        long next = await context.Database
            .SqlQueryRaw<long>("SELECT nextval('invoice_number_seq') AS \"Value\"")
            .SingleAsync(cancellationToken);

        return $"F-{next:D8}";
    }
}
