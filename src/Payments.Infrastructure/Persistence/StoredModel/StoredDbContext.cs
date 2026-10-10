using Payments.Infrastructure.Inbox;
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
        modelBuilder.AddInboxModel();

        modelBuilder.Entity<PaymentLineStoredModel>()
            .HasOne<PaymentStoredModel>()
            .WithMany()
            .HasForeignKey(l => l.PaymentId)
            .OnDelete(DeleteBehavior.Cascade);

        // An invoice number is issued once.
        modelBuilder.Entity<PaymentStoredModel>()
            .HasIndex(p => p.InvoiceNumber)
            .IsUnique()
            .HasFilter("\"invoiceNumber\" IS NOT NULL");

        // One payment per originating order.
        modelBuilder.Entity<PaymentStoredModel>()
            .HasIndex(p => new { p.SourceId, p.SourceType })
            .IsUnique()
            .HasFilter("\"sourceId\" IS NOT NULL");
    }

    public void Migrate()
    {
        Database.Migrate();
    }
}
