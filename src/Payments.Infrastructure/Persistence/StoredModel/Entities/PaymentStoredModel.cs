using Payments.Domain.Payments;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Payments.Infrastructure.Persistence.StoredModel.Entities;

[Table("payment")]
public class PaymentStoredModel
{
    [Key]
    [Column("paymentId")]
    public Guid PaymentId { get; set; }

    [Required]
    [Column("createdOn")]
    public DateTime CreatedOn { get; set; }

    [Column("paidOn")]
    public DateTime? PaidOn { get; set; }

    [Column("status")]
    [MaxLength(25)]
    [Required]
    public string Status { get; private set; }

    [Column("sourceId")]
    public Guid? SourceId { get; set; }

    [Column("sourceType")]
    [MaxLength(25)]
    public string? SourceType { get; set; }

    [Required]
    [Column("amount", TypeName = "decimal(18,2)")]
    public double Amount { get; set; }

    [Column("customerName")]
    [MaxLength(200)]
    public string? CustomerName { get; set; }

    [Column("customerDocument")]
    [MaxLength(50)]
    public string? CustomerDocument { get; set; }

    [Column("method")]
    [MaxLength(25)]
    public string? Method { get; set; }

    [Column("reference")]
    [MaxLength(100)]
    public string? Reference { get; set; }

    [Column("amountReceived", TypeName = "decimal(18,2)")]
    public decimal? AmountReceived { get; set; }

    [Column("changeGiven", TypeName = "decimal(18,2)")]
    public decimal? ChangeGiven { get; set; }

    [Column("collectedBy")]
    public Guid? CollectedBy { get; set; }

    [Column("invoiceNumber")]
    [MaxLength(20)]
    public string? InvoiceNumber { get; set; }
}
