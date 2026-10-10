using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Payments.Infrastructure.Persistence.StoredModel.Entities;

[Table("paymentLine")]
public class PaymentLineStoredModel
{
    [Key]
    [Column("paymentLineId")]
    public Guid Id { get; set; }

    [Column("paymentId")]
    public Guid PaymentId { get; set; }

    [Column("productId")]
    public Guid ProductId { get; set; }

    [Column("productName")]
    [MaxLength(250)]
    public string ProductName { get; set; } = string.Empty;

    [Column("quantity")]
    public int Quantity { get; set; }

    [Column("unitPrice", TypeName = "decimal(18,2)")]
    public decimal UnitPrice { get; set; }
}
