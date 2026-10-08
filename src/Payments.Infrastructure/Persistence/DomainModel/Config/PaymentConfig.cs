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

        var statusConverter = new ValueConverter<PaymentStatus, string>(
            v => v.ToString(),
            v => (PaymentStatus)Enum.Parse(typeof(PaymentStatus), v));

        builder.Property(p => p.Status)
            .HasColumnName("status")
            .HasConversion(statusConverter);
    }
}