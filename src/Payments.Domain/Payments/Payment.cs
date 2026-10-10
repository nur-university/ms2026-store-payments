using Joseco.DDD.Core.Abstractions;
using Joseco.DDD.Core.Results;
using Payments.Domain.Payments.Events;

namespace Payments.Domain.Payments;

/// <summary>
/// A charge order (orden de cobro): created pending when a sale is ready to be paid, and collected later at the
/// cashier with cash, a bank transfer or a card terminal (POS). No payment provider is involved.
/// </summary>
public class Payment : AggregateRoot
{
    public DateTime CreatedOn { get; set; }
    public DateTime? PaidOn { get; set; }
    public PaymentStatus Status { get; private set; }

    public Guid? SourceId { get; set; }
    public string? SourceType { get; set; }

    public decimal Amount { get; set; }

    /// <summary>The customer named on the sales note (walk-in customers have no account).</summary>
    public string? CustomerName { get; private set; }
    public string? CustomerDocument { get; private set; }

    public PaymentMethod? Method { get; private set; }

    /// <summary>Transfer number or POS voucher/authorization code; not used for cash.</summary>
    public string? Reference { get; private set; }

    /// <summary>Cash handed over by the customer (equals <see cref="Amount"/> for the other methods).</summary>
    public decimal? AmountReceived { get; private set; }
    public decimal? ChangeGiven { get; private set; }

    /// <summary>The cashier (user id) who collected the payment.</summary>
    public Guid? CollectedBy { get; private set; }

    /// <summary>Number of the invoice issued when the payment was collected.</summary>
    public string? InvoiceNumber { get; private set; }

    private readonly List<PaymentLine> _lines = [];

    /// <summary>The products being paid. Charge orders created before the detail was kept have none.</summary>
    public IReadOnlyCollection<PaymentLine> Lines => _lines;

    public void AddLine(Guid productId, string productName, int quantity, decimal unitPrice)
    {
        if (Status != PaymentStatus.Pending)
            throw new InvalidOperationException("Lines can only be added to a pending payment.");

        _lines.Add(new PaymentLine(Id, productId, productName, quantity, unitPrice));
    }

    public Payment(decimal amount, Guid? sourceId, string? sourceType,
        string? customerName = null, string? customerDocument = null) : base(
        Guid.NewGuid())
    {
        Amount = amount;
        CreatedOn = DateTime.UtcNow;
        Status = PaymentStatus.Pending;
        SourceId = sourceId;
        SourceType = sourceType;
        CustomerName = customerName;
        CustomerDocument = customerDocument;

        AddDomainEvent(new PendingPaymentCreated(Id, SourceId, SourceType));
    }

    /// <summary>
    /// Checks what the cashier typed without changing anything. The invoice number is only requested after this passes,
    /// so rejected attempts do not leave gaps in the invoice numbering.
    /// </summary>
    /// <exception cref="DomainException">The data does not allow collecting the payment.</exception>
    public void ValidateCollection(PaymentMethod method, string? reference, decimal? amountReceived)
    {
        switch (method)
        {
            case PaymentMethod.Cash:
                if (amountReceived is null || amountReceived < Amount)
                    throw new DomainException(PaymentErrors.CashReceivedInsufficient(Amount, amountReceived));
                break;

            case PaymentMethod.BankTransfer:
            case PaymentMethod.Card:
                if (string.IsNullOrWhiteSpace(reference))
                    throw new DomainException(PaymentErrors.ReferenceRequired(method));
                break;

            default:
                throw new DomainException(PaymentErrors.UnknownMethod());
        }
    }

    /// <summary>Registers the money received at the cashier and issues the invoice.</summary>
    /// <param name="amountReceived">Required for cash (it must cover the amount; the change is computed).</param>
    /// <param name="reference">Required for bank transfers and card payments.</param>
    public void Collect(PaymentMethod method, string? reference, decimal? amountReceived, Guid collectedBy, string invoiceNumber)
    {
        if (Status != PaymentStatus.Pending)
            throw new InvalidOperationException("Payment can only be collected if it is pending.");

        ValidateCollection(method, reference, amountReceived);

        if (method == PaymentMethod.Cash)
        {
            AmountReceived = amountReceived;
            ChangeGiven = amountReceived - Amount;
            Reference = null;
        }
        else
        {
            AmountReceived = Amount;
            ChangeGiven = 0;
            Reference = reference!.Trim();
        }

        Method = method;
        CollectedBy = collectedBy;
        InvoiceNumber = invoiceNumber;
        Status = PaymentStatus.Completed;
        PaidOn = DateTime.UtcNow;

        AddDomainEvent(new PaymentCompleted(Id, SourceId, SourceType, invoiceNumber, method.ToString()));
    }

    /// <summary>Withdraws a pending payment (for example because its order was canceled); it can no longer be collected.</summary>
    public void Cancel()
    {
        if (Status != PaymentStatus.Pending)
            throw new InvalidOperationException("Payment can only be canceled if it is pending.");
        Status = PaymentStatus.Cancelled;
    }

    private Payment() { } // For EF Core
}
