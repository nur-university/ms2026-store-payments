using Joseco.Communication.External.Contracts.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Nur.Store2026.Integration.Orders;
using Payments.Application.Payments.CreatePayment;
using Payments.Domain.Payments;
using Payments.Infrastructure.Inbox;
using Payments.Infrastructure.Persistence;
using Payments.Infrastructure.Persistence.DomainModel;

namespace Payments.Test.Infrastructure;

/// <summary>The inbox decorator and the handler it protects, against a real DbContext (SQLite in memory).</summary>
public sealed class IdempotentConsumerTests : IDisposable
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");
    private readonly DomainDbContext _dbContext;
    private readonly UnitOfWork _unitOfWork;
    private readonly PaymentRepositoryStub _repository;

    // The real repository is internal to Payments.Infrastructure and tests do not need more than these two operations.
    private sealed class PaymentRepositoryStub(DomainDbContext db) : IPaymentRepository
    {
        public async Task AddAsync(Payment entity) => await db.Payments.AddAsync(entity);
        public Task<Payment?> GetByIdAsync(Guid id, bool readOnly = false) => db.Payments.FirstOrDefaultAsync(p => p.Id == id);
        public Task<Payment?> GetBySourceAsync(Guid sourceId, string sourceType) =>
            db.Payments.FirstOrDefaultAsync(p => p.SourceId == sourceId && p.SourceType == sourceType);
    }

    public IdempotentConsumerTests()
    {
        _connection.Open();
        _dbContext = new DomainDbContext(new DbContextOptionsBuilder<DomainDbContext>().UseSqlite(_connection).Options);
        _dbContext.Database.EnsureCreated();
        _unitOfWork = new UnitOfWork(_dbContext, Mock.Of<MediatR.IMediator>());
        _repository = new PaymentRepositoryStub(_dbContext);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        _connection.Dispose();
    }

    private IdempotentConsumer<OrderReserved> Consumer(IIntegrationMessageConsumer<OrderReserved> inner) =>
        new(inner, _dbContext, NullLogger<IdempotentConsumer<OrderReserved>>.Instance);

    private int Count(string table) => _dbContext.Database.SqlQueryRaw<int>($"SELECT COUNT(*) AS Value FROM {table}").Single();

    [Fact]
    public async Task SameMessageDeliveredTwice_IsHandledOnce()
    {
        CreatePaymentHandler handler = new(_repository, _unitOfWork);
        OrderReserved message = new(Guid.NewGuid(), 100m);
        var consumer = Consumer(handler);

        await consumer.HandleAsync(message, CancellationToken.None);
        await consumer.HandleAsync(message, CancellationToken.None);

        Assert.Equal(1, Count("payment"));
        Assert.Equal(1, Count("inboxMessage"));
    }

    [Fact]
    public async Task SameOrderRepublishedWithANewMessageId_StillCreatesOnePayment()
    {
        Guid orderId = Guid.NewGuid();
        CreatePaymentHandler handler = new(_repository, _unitOfWork);

        await Consumer(handler).HandleAsync(new OrderReserved(orderId, 100m), CancellationToken.None);
        await Consumer(handler).HandleAsync(new OrderReserved(orderId, 100m), CancellationToken.None);

        Assert.Equal(1, Count("payment"));
        Assert.Equal(2, Count("inboxMessage"));
    }

    [Fact]
    public async Task WhenTheHandlerFails_NothingIsRecorded_SoTheRetryRunsAgain()
    {
        Mock<IIntegrationMessageConsumer<OrderReserved>> failing = new();
        failing.Setup(c => c.HandleAsync(It.IsAny<OrderReserved>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));
        OrderReserved message = new(Guid.NewGuid(), 100m);

        await Assert.ThrowsAsync<InvalidOperationException>(() => Consumer(failing.Object).HandleAsync(message, CancellationToken.None));
        _dbContext.ChangeTracker.Clear(); // what a new DI scope gives the retry

        CreatePaymentHandler handler = new(_repository, _unitOfWork);
        await Consumer(handler).HandleAsync(message, CancellationToken.None);

        Assert.Equal(1, Count("payment"));
        Assert.Equal(1, Count("inboxMessage"));
    }

    [Fact]
    public async Task WhenTheHandlerSavesNothing_TheMessageIsStillRecorded()
    {
        Mock<IIntegrationMessageConsumer<OrderReserved>> ignoring = new();
        OrderReserved message = new(Guid.NewGuid(), 100m);

        await Consumer(ignoring.Object).HandleAsync(message, CancellationToken.None);
        await Consumer(ignoring.Object).HandleAsync(message, CancellationToken.None);

        Assert.Equal(1, Count("inboxMessage"));
        ignoring.Verify(c => c.HandleAsync(It.IsAny<OrderReserved>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
