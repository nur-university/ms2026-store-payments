using Joseco.DDD.Core.Results;
using MediatR;
using Payments.Domain.Payments;

namespace Payments.Application.Payments.Queries;

public record PaymentLineDto(Guid ProductId, string ProductName, int Quantity, decimal UnitPrice, decimal Subtotal);

public record PaymentDto(
    Guid PaymentId,
    Guid? OrderId,
    decimal Amount,
    string Status,
    string? CustomerName,
    string? CustomerDocument,
    DateTime CreatedOn,
    DateTime? PaidOn,
    string? Method,
    string? Reference,
    decimal? ChangeGiven,
    string? InvoiceNumber,
    IReadOnlyList<PaymentLineDto> Lines)
{
    public static PaymentDto From(Payment p) => new(
        p.Id, p.SourceId, p.Amount, p.Status.ToString(), p.CustomerName, p.CustomerDocument, p.CreatedOn, p.PaidOn,
        p.Method?.ToString(), p.Reference, p.ChangeGiven, p.InvoiceNumber, ToLines(p));

    internal static IReadOnlyList<PaymentLineDto> ToLines(Payment p) => p.Lines
        .Select(l => new PaymentLineDto(l.ProductId, l.ProductName, l.Quantity, l.UnitPrice, l.Subtotal))
        .ToList();
}

public record InvoiceDto(
    string InvoiceNumber,
    DateTime IssuedOn,
    Guid? OrderId,
    string? CustomerName,
    string? CustomerDocument,
    decimal Total,
    string Method,
    string? Reference,
    decimal AmountReceived,
    decimal ChangeGiven,
    Guid? CollectedBy,
    IReadOnlyList<PaymentLineDto> Lines);

public record PagedPayments(IReadOnlyList<PaymentDto> Items, int Page, int PageSize, int Total);

// ---- the cashier's queue

public record GetPendingPaymentsQuery(int Page = 1, int PageSize = 20) : IRequest<Result<PagedPayments>>;

internal class GetPendingPaymentsHandler(IPaymentRepository payments) : IRequestHandler<GetPendingPaymentsQuery, Result<PagedPayments>>
{
    public async Task<Result<PagedPayments>> Handle(GetPendingPaymentsQuery request, CancellationToken cancellationToken)
    {
        int page = Math.Max(1, request.Page);
        int pageSize = Math.Clamp(request.PageSize, 1, 100);

        (IReadOnlyList<Payment> items, int total) = await payments.GetPendingAsync((page - 1) * pageSize, pageSize);

        return new PagedPayments(items.Select(PaymentDto.From).ToList(), page, pageSize, total);
    }
}

// ---- one charge order

public record GetPaymentByOrderQuery(Guid OrderId) : IRequest<Result<PaymentDto>>;

internal class GetPaymentByOrderHandler(IPaymentRepository payments) : IRequestHandler<GetPaymentByOrderQuery, Result<PaymentDto>>
{
    public async Task<Result<PaymentDto>> Handle(GetPaymentByOrderQuery request, CancellationToken cancellationToken)
    {
        Payment? payment = await payments.GetBySourceAsync(request.OrderId, "orders");

        return payment == null
            ? Result.Failure<PaymentDto>(PaymentErrors.NotFound(request.OrderId, "orders"))
            : PaymentDto.From(payment);
    }
}

// ---- the invoice issued when the payment was collected

public record GetInvoiceByOrderQuery(Guid OrderId) : IRequest<Result<InvoiceDto>>;

internal class GetInvoiceByOrderHandler(IPaymentRepository payments) : IRequestHandler<GetInvoiceByOrderQuery, Result<InvoiceDto>>
{
    public async Task<Result<InvoiceDto>> Handle(GetInvoiceByOrderQuery request, CancellationToken cancellationToken)
    {
        Payment? payment = await payments.GetBySourceAsync(request.OrderId, "orders");
        if (payment == null)
        {
            return Result.Failure<InvoiceDto>(PaymentErrors.NotFound(request.OrderId, "orders"));
        }

        if (payment.Status != PaymentStatus.Completed || payment.InvoiceNumber == null)
        {
            return Result.Failure<InvoiceDto>(PaymentErrors.NotInvoiced(request.OrderId));
        }

        return new InvoiceDto(
            payment.InvoiceNumber, payment.PaidOn!.Value, payment.SourceId, payment.CustomerName, payment.CustomerDocument,
            payment.Amount, payment.Method!.Value.ToString(), payment.Reference,
            payment.AmountReceived ?? payment.Amount, payment.ChangeGiven ?? 0, payment.CollectedBy, PaymentDto.ToLines(payment));
    }
}
