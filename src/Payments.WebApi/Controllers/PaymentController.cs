using Joseco.DDD.Core.Results;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Payments.Application.Payments.CollectPayment;
using Payments.Application.Payments.Queries;
using Payments.Domain.Payments;
using Nur.Store2026.Access.Payments.Permissions;
using Payments.WebApi.Infraestructure;

namespace Payments.WebApi.Controllers;

/// <summary>
/// The cashier's desk. A charge order is created pending when a sale is ready; the customer pays at the cashier with
/// cash, a bank transfer or a card terminal (POS), and an invoice is issued. No payment provider is involved.
/// </summary>
[Route("api/[controller]")]
[ApiController]
[Authorize]
public class PaymentController : ControllerBase
{
    private readonly IMediator _mediator;

    public PaymentController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <param name="Method">Cash, BankTransfer or Card.</param>
    /// <param name="Reference">Transfer number or POS voucher/authorization code (required unless cash).</param>
    /// <param name="AmountReceived">Cash handed over (required for cash; the change is computed).</param>
    public record CollectPaymentRequest(string Method, string? Reference, decimal? AmountReceived);

    /// <summary>The charge orders waiting to be paid, oldest first.</summary>
    [HttpGet("pending")]
    [Authorize(Policy = PaymentsPermission.PaymentRead)]
    public async Task<IActionResult> GetPending([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        Result<PagedPayments> result = await _mediator.Send(new GetPendingPaymentsQuery(page, pageSize));
        return result.IsSuccess ? Ok(result.Value) : this.ToProblem(result.Error);
    }

    /// <summary>The charge order of a sale.</summary>
    [HttpGet("orders/{orderId:guid}")]
    [Authorize(Policy = PaymentsPermission.PaymentRead)]
    public async Task<IActionResult> GetByOrder(Guid orderId)
    {
        Result<PaymentDto> result = await _mediator.Send(new GetPaymentByOrderQuery(orderId));
        return result.IsSuccess ? Ok(result.Value) : this.ToProblem(result.Error);
    }

    /// <summary>Registers the money received at the cashier and issues the invoice.</summary>
    [HttpPost("orders/{orderId:guid}/collect")]
    [Authorize(Policy = PaymentsPermission.PaymentCollect)]
    public async Task<IActionResult> Collect(Guid orderId, [FromBody] CollectPaymentRequest request)
    {
        if (!User.TryGetUserId(out Guid cashierId))
        {
            return Unauthorized();
        }

        if (!Enum.TryParse(request.Method, ignoreCase: true, out PaymentMethod method) || !Enum.IsDefined(method))
        {
            return this.ToProblem(PaymentErrors.UnknownMethod());
        }

        Result<CollectedPayment> result = await _mediator.Send(
            new CollectPaymentCommand(orderId, "orders", method, request.Reference, request.AmountReceived, cashierId));

        return result.IsSuccess ? Ok(result.Value) : this.ToProblem(result.Error);
    }

    /// <summary>The invoice issued when the payment of the sale was collected.</summary>
    [HttpGet("orders/{orderId:guid}/invoice")]
    [Authorize(Policy = PaymentsPermission.PaymentRead)]
    public async Task<IActionResult> GetInvoice(Guid orderId)
    {
        Result<InvoiceDto> result = await _mediator.Send(new GetInvoiceByOrderQuery(orderId));
        return result.IsSuccess ? Ok(result.Value) : this.ToProblem(result.Error);
    }
}
