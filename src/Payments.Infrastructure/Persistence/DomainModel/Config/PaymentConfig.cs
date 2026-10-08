using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Payments.Domain.Payments;

namespace Payments.Infrastructure.Persistence.DomainModel.Config;

internal class PaymentConfig  : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("payment");
        builder.HasKey(p => p.Id);

        builder.Property(x => x.Id)
            .HasColumnName("paymentId");

        builder.Property(p => p.CreatedOn)
            .HasColumnName("createdOn");

        builder.Property(p => p.SourceId)
            .HasColumnName("sourceId");

        builder.Property(p => p.SourceType)
            .HasColumnName("sourceType");

        builder.Property(p => p.PaidOn)
            .HasColumnName("paidOn");

        builder.Property(p => p.Amount)
            .HasColumnName("amount");

        builder.Property(p => p.CustomerName).HasColumnName("customerName").HasMaxLength(200);
        builder.Property(p => p.CustomerDocument).HasColumnName("customerDocument").HasMaxLength(50);

        builder.Property(p => p.Method)
            .HasColumnName("method")
            .HasMaxLength(25)
            .HasConversion(m => m == null ? null : m.ToString(), s => s == null ? null : (PaymentMethod?)Enum.Parse<PaymentMethod>(s));
        builder.Property(p => p.Reference).HasColumnName("reference").HasMaxLength(100);
        builder.Property(p => p.AmountReceived).HasColumnName("amountReceived");
        builder.Property(p => p.ChangeGiven).HasColumnName("changeGiven");
        builder.Property(p => p.CollectedBy).HasColumnName("collectedBy");
        builder.Property(p => p.InvoiceNumber).HasColumnName("invoiceNumber").HasMaxLength(20);

        var statusConverter = new ValueConverter<PaymentStatus, string>(
            v => v.ToString(),
            v => (PaymentStatus)Enum.Parse(typeof(PaymentStatus), v));

        builder.Property(p => p.Status)
            .HasColumnName("status")
            .HasConversion(statusConverter);
    }
}