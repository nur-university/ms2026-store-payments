using Payments.Infrastructure.Inbox;
using Joseco.DDD.Core.Abstractions;
using Joseco.Outbox.Contracts.Model;
using Joseco.Outbox.EFCore.Persistence;
using Microsoft.EntityFrameworkCore;
using Payments.Domain.Payments;

namespace Payments.Infrastructure.Persistence.DomainModel;

internal class DomainDbContext(DbContextOptions<DomainDbContext> options) : DbContext(options)
{
    public DbSet<Payment> Payments { get; set; }
    public DbSet<OutboxMessage<DomainEvent>> OutboxMessages { get; set; }
    public DbSet<InboxMessage> InboxMessages { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(DomainDbContext).Assembly);
        modelBuilder.AddOutboxModel<DomainEvent>();
        modelBuilder.AddInboxModel();

        base.OnModelCreating(modelBuilder);

        modelBuilder.Ignore<DomainEvent>();

        if (Database.IsNpgsql())
        {
            // Optimistic concurrency: two workers changing the same row at once must not overwrite each other.
            modelBuilder.Entity<Payment>().Property<uint>("xmin")
                .HasColumnName("xmin").HasColumnType("xid").ValueGeneratedOnAddOrUpdate().IsConcurrencyToken();
        }
    }
}
