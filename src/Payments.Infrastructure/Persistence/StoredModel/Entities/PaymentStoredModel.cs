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
}
