using Joseco.DDD.Core.Results;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Payments.Application.Payments.CompletePayment;
using Payments.Application.Payments.FailPayment;

namespace Payments.WebApi.Controllers;

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

    public record FailPaymentRequest(string Reason);

    /// <summary>Confirms the pending payment of an order (stands in for the payment provider's confirmation).</summary>
    [HttpPost("orders/{orderId:guid}/complete")]
    public async Task<IActionResult> CompleteOrderPayment(Guid orderId)
    {
        Result<Guid> result = await _mediator.Send(new CompletePaymentCommand(orderId, "orders"));
        return result.IsSuccess ? Ok(result.Value) : NotFound(result.Error);
    }

    /// <summary>Rejects the pending payment of an order (stands in for a declined payment).</summary>
    [HttpPost("orders/{orderId:guid}/fail")]
    public async Task<IActionResult> FailOrderPayment(Guid orderId, [FromBody] FailPaymentRequest request)
    {
        Result<Guid> result = await _mediator.Send(new FailPaymentCommand(orderId, "orders", request.Reason));
        return result.IsSuccess ? Ok(result.Value) : NotFound(result.Error);
    }
}
