using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore;
using Joseco.DDD.Core.Abstractions;
using Joseco.Outbox.EFCore.Persistence;
using Payments.Infrastructure.Persistence.StoredModel.Entities;

namespace Payments.Infrastructure.Persistence.StoredModel;

internal class StoredDbContext(DbContextOptions<StoredDbContext> options) : DbContext(options), IDatabase
{
    public DbSet<PaymentStoredModel> Payment { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.AddOutboxModel<DomainEvent>();
    }

    public void Migrate()
    {
        Database.Migrate();
    }
}
