using Joseco.DDD.Core.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Payments.Domain.Payments.Events;

public record PendingPaymentCreated(Guid PaymentId, Guid? SourceId, string SourceType) : DomainEvent;
