using MediatR;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Payments.Domain.Payments;
using Payments.Domain.Payments.Events;
using Payments.Infrastructure.Persistence;
using Payments.Infrastructure.Persistence.DomainModel;

namespace Payments.Test.Infrastructure;

/// <summary>
/// Integration tests for the UnitOfWork against a real DbContext (SQLite in memory) and a real MediatR pipeline.
/// </summary>
public sealed class UnitOfWorkTests : IDisposable
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");
    private readonly List<INotificationHandler<PaymentCompleted>> _completedHandlers = [];
    private readonly List<INotificationHandler<PaymentFailed>> _failedHandlers = [];
    private readonly DomainDbContext _dbContext;
    private readonly UnitOfWork _unitOfWork;
    private int _saveCount;

    public UnitOfWorkTests()
    {
        _connection.Open();
        _dbContext = new DomainDbContext(
            new DbContextOptionsBuilder<DomainDbContext>().UseSqlite(_connection).Options);
        _dbContext.Database.EnsureCreated();
        _dbContext.SavingChanges += (_, _) => _saveCount++;

        ServiceCollection services = new();
        services.AddMediatR(c => c.RegisterServicesFromAssemblyContaining<UnitOfWorkTests>());
        services.AddSingleton<IEnumerable<INotificationHandler<PaymentCompleted>>>(_completedHandlers);
        services.AddSingleton<IEnumerable<INotificationHandler<PaymentFailed>>>(_failedHandlers);
        IMediator mediator = services.BuildServiceProvider().GetRequiredService<IMediator>();

        _unitOfWork = new UnitOfWork(_dbContext, mediator);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        _connection.Dispose();
    }

    private Payment TrackedPendingPayment()
    {
        Payment payment = new(100m, Guid.NewGuid(), "orders");
        payment.ClearDomainEvents();
        _dbContext.Payments.Add(payment);
        return payment;
    }

    private int PersistedPayments() => _dbContext.Database.SqlQueryRaw<int>("SELECT COUNT(*) AS Value FROM payment").Single();

    private sealed class Handler<T>(Func<T, Task> action) : INotificationHandler<T> where T : INotification
    {
        public Task Handle(T notification, CancellationToken cancellationToken) => action(notification);
    }

    [Fact]
    public async Task Commit_PublishesEventsAndSavesOnce()
    {
        Payment payment = TrackedPendingPayment();
        payment.MarkAsPaid();
        List<Guid> seen = [];
        _completedHandlers.Add(new Handler<PaymentCompleted>(e => { seen.Add(e.PaymentId); return Task.CompletedTask; }));

        await _unitOfWork.CommitAsync();

        Assert.Equal([payment.Id], seen);
        Assert.Equal(1, _saveCount);
        Assert.Equal(1, PersistedPayments());
        Assert.Empty(payment.DomainEvents);
    }

    [Fact]
    public async Task Commit_CalledFromInsideAHandler_DoesNotSaveEarly()
    {
        Payment payment = TrackedPendingPayment();
        payment.MarkAsPaid();
        int savesSeenByHandler = -1;
        _completedHandlers.Add(new Handler<PaymentCompleted>(async _ =>
        {
            await _unitOfWork.CommitAsync(); // legacy-style handler: must be a no-op
            savesSeenByHandler = _saveCount;
        }));

        await _unitOfWork.CommitAsync();

        Assert.Equal(0, savesSeenByHandler);
        Assert.Equal(1, _saveCount);
    }

    [Fact]
    public async Task Commit_DrainsEventsRaisedByHandlers()
    {
        Payment first = TrackedPendingPayment();
        Payment second = TrackedPendingPayment();
        first.MarkAsPaid();
        List<string> order = [];
        _completedHandlers.Add(new Handler<PaymentCompleted>(_ =>
        {
            order.Add("completed");
            second.MarkAsFailed("cascade"); // raises a new event while events are being published
            return Task.CompletedTask;
        }));
        _failedHandlers.Add(new Handler<PaymentFailed>(e => { order.Add("failed:" + e.Reason); return Task.CompletedTask; }));

        await _unitOfWork.CommitAsync();

        Assert.Equal(["completed", "failed:cascade"], order);
        Assert.Equal(1, _saveCount);
        Assert.Equal(2, PersistedPayments());
    }

    [Fact]
    public async Task Commit_WhenAHandlerFails_PersistsNothingEvenOnALaterCommit()
    {
        Payment payment = TrackedPendingPayment();
        payment.MarkAsPaid();
        _completedHandlers.Add(new Handler<PaymentCompleted>(_ => throw new InvalidOperationException("boom")));

        await Assert.ThrowsAsync<InvalidOperationException>(() => _unitOfWork.CommitAsync());

        // Same scope, e.g. the code path that records a failure after catching the exception.
        await _unitOfWork.CommitAsync();

        Assert.Equal(0, PersistedPayments());
        Assert.Empty(_dbContext.ChangeTracker.Entries());
    }

    [Fact]
    public async Task Commit_AfterAFailedCommit_WorksNormally()
    {
        Payment broken = TrackedPendingPayment();
        broken.MarkAsPaid();
        _completedHandlers.Add(new Handler<PaymentCompleted>(_ => throw new InvalidOperationException("boom")));
        await Assert.ThrowsAsync<InvalidOperationException>(() => _unitOfWork.CommitAsync());
        _completedHandlers.Clear();

        TrackedPendingPayment();
        await _unitOfWork.CommitAsync();

        Assert.Equal(1, PersistedPayments());
    }

    [Fact]
    public async Task Commit_WithAnEventCycle_Throws()
    {
        Payment payment = TrackedPendingPayment();
        payment.MarkAsPaid();
        // Every PaymentCompleted creates yet another payment that completes: never settles.
        _completedHandlers.Add(new Handler<PaymentCompleted>(_ =>
        {
            TrackedPendingPayment().MarkAsPaid();
            return Task.CompletedTask;
        }));

        InvalidOperationException ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _unitOfWork.CommitAsync());

        Assert.Contains("cascaded", ex.Message);
        Assert.Equal(0, PersistedPayments());
    }
}
