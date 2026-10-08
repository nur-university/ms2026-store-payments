using Joseco.DDD.Core.Results;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Payments.Application.Payments.CreatePayment;

public class CreatePaymentCommand : IRequest<Result<Guid>>
{
    public Guid? SourceId { get; set; }
    public string SourceType { get; set; }
    public decimal Amount { get; set; }
    public CreatePaymentCommand(decimal amount, Guid? sourceId, string sourceType)
    {
        Amount = amount;
        SourceId = sourceId;
        SourceType = sourceType;
    }
}
