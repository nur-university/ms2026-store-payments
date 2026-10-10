using MediatR;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Payments.Domain.Payments;
using Payments.Domain.Payments.Events;
using Payments.Infrastructure.Persistence;
using Payments.Infrastructure.Persistence.DomainModel;
using System.Diagnostics.Metrics;

namespace Payments.Test.Infrastructure;

/// <summary>
/// The business metrics are taken from the status changes the real UnitOfWork persists (SQLite in memory) and are
/// only recorded when the commit succeeded. Amounts are unique per test because the meter is process-wide and
/// other test classes run in parallel.
/// </summary>
public sealed class PaymentsMetricsTests : IDisposable
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");
    private readonly List<INotificationHandler<PaymentCompleted>> _completedHandlers = [];
    private readonly DomainDbContext _dbContext;
    private readonly UnitOfWork _unitOfWork;

    private sealed record Measurement(string Instrument, double Value, Dictionary<string, object?> Tags);

    public PaymentsMetricsTests()
    {
        _connection.Open();
        _dbContext = new DomainDbContext(
            new DbContextOptionsBuilder<DomainDbContext>().UseSqlite(_connection).Options);
        _dbContext.Database.EnsureCreated();

        ServiceCollection services = new();
        services.AddMediatR(c => c.RegisterServicesFromAssemblyContaining<PaymentsMetricsTests>());
        services.AddSingleton<IEnumerable<INotificationHandler<PaymentCompleted>>>(_completedHandlers);
        _unitOfWork = new UnitOfWork(_dbContext, services.BuildServiceProvider().GetRequiredService<IMediator>());
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        _connection.Dispose();
    }

    private sealed class Handler<T>(Func<T, Task> action) : INotificationHandler<T> where T : INotification
    {
        public Task Handle(T notification, CancellationToken cancellationToken) => action(notification);
    }

    private static MeterListener Listen(List<Measurement> into)
    {
        var listener = new MeterListener
        {
            InstrumentPublished = (instrument, l) =>
            {
                if (instrument.Meter.Name == "Store.Payments")
                {
                    l.EnableMeasurementEvents(instrument);
                }
            }
        };
        listener.SetMeasurementEventCallback<long>((i, v, tags, _) => Add(into, i, v, tags));
        listener.SetMeasurementEventCallback<double>((i, v, tags, _) => Add(into, i, v, tags));
        listener.Start();
        return listener;
    }

    private static void Add(List<Measurement> into, Instrument instrument, double value, ReadOnlySpan<KeyValuePair<string, object?>> tags)
    {
        var dict = new Dictionary<string, object?>();
        foreach (var tag in tags)
        {
            dict[tag.Key] = tag.Value;
        }
        lock (into)
        {
            into.Add(new Measurement(instrument.Name, value, dict));
        }
    }

    [Fact]
    public async Task Collecting_RecordsTransitionAndCollectedAmountByMethod()
    {
        var seen = new List<Measurement>();
        using MeterListener listener = Listen(seen);

        Payment payment = new(1234.56m, Guid.NewGuid(), "orders");
        payment.ClearDomainEvents();
        _dbContext.Payments.Add(payment);
        await _unitOfWork.CommitAsync();

        payment.PayInCash();
        await _unitOfWork.CommitAsync();

        lock (seen)
        {
            Measurement collected = Assert.Single(seen, m => m.Instrument == "store.payments.collected.amount" && m.Value == 1234.56);
            Assert.Equal("Cash", collected.Tags["method"]);
            Assert.Contains(seen, m => m.Instrument == "store.payments.transitions"
                && (string?)m.Tags["status"] == "Completed" && (string?)m.Tags["previous_status"] == "Pending");
            Assert.Contains(seen, m => m.Instrument == "store.payments.transitions"
                && (string?)m.Tags["status"] == "Pending" && (string?)m.Tags["previous_status"] == "none");
        }
    }

    [Fact]
    public async Task FailedCommit_RecordsNothing()
    {
        Payment payment = new(4321.09m, Guid.NewGuid(), "orders");
        payment.ClearDomainEvents();
        _dbContext.Payments.Add(payment);
        await _unitOfWork.CommitAsync();
        payment.PayInCash();
        _completedHandlers.Add(new Handler<PaymentCompleted>(_ => throw new InvalidOperationException("handler failed")));

        var seen = new List<Measurement>();
        using MeterListener listener = Listen(seen);

        await Assert.ThrowsAsync<InvalidOperationException>(() => _unitOfWork.CommitAsync());

        lock (seen)
        {
            Assert.DoesNotContain(seen, m => m.Instrument == "store.payments.collected.amount" && m.Value == 4321.09);
        }
    }
}
