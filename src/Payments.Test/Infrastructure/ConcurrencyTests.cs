using Microsoft.EntityFrameworkCore;
using Payments.Domain.Payments;
using Payments.Infrastructure.Persistence.DomainModel;

namespace Payments.Test.Infrastructure;

/// <summary>Runs only when PAYMENTS_TEST_POSTGRES holds a connection string to a migrated StorePayments database.</summary>
public sealed class PostgresFactAttribute : FactAttribute
{
    public const string EnvironmentVariable = "PAYMENTS_TEST_POSTGRES";

    public PostgresFactAttribute()
    {
        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable(EnvironmentVariable)))
        {
            Skip = $"Set {EnvironmentVariable} to a connection string of a migrated StorePayments database to run it.";
        }
    }
}

public class ConcurrencyTests
{
    private static DomainDbContext NewContext() => new(
        new DbContextOptionsBuilder<DomainDbContext>()
            .UseNpgsql(Environment.GetEnvironmentVariable(PostgresFactAttribute.EnvironmentVariable))
            .Options);

    [PostgresFact]
    public async Task TwoWritersOfTheSameRow_SecondOneGetsAConcurrencyException()
    {
        Payment payment = new(100m, Guid.NewGuid(), "orders");
        payment.ClearDomainEvents();

        await using (DomainDbContext setup = NewContext())
        {
            setup.Payments.Add(payment);
            await setup.SaveChangesAsync();
        }

        try
        {
            await using DomainDbContext first = NewContext();
            await using DomainDbContext second = NewContext();
            Payment seenByFirst = await first.Payments.SingleAsync(p => p.Id == payment.Id);
            Payment seenBySecond = await second.Payments.SingleAsync(p => p.Id == payment.Id);

            seenByFirst.PayInCash();
            await first.SaveChangesAsync();

            seenBySecond.Cancel(); // still sees the row as Pending
            await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => second.SaveChangesAsync());
        }
        finally
        {
            await using DomainDbContext cleanup = NewContext();
            await cleanup.Payments.Where(p => p.Id == payment.Id).ExecuteDeleteAsync();
        }
    }
}
