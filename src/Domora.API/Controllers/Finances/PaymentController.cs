using Domora.API.Common.Authorization;
using Domora.API.Finances.Payments;
using Domora.Application.Finance.Commands.ReceivePayment;
using Domora.Application.Finance.Commands.VoidPayment;
using Domora.Domain.Common;
using Microsoft.AspNetCore.Mvc;

namespace Domora.API.Controllers.Finances;

[ApiController]
[Route("api/finances/payments")]

public sealed class PaymentController : ControllerBase
{
    private readonly ReceivePaymentHandler _receiveHandler;
    private readonly VoidPaymentHandler _voidHandler;

    public PaymentController(
        ReceivePaymentHandler receiveHandler,
        VoidPaymentHandler voidHandler
    )
    {
        _receiveHandler = receiveHandler;
        _voidHandler = voidHandler;
    }

    /// <summary>Records a payment received by the current organization.</summary>
    [RequireOrganizationAccess]
    [HttpPost]
    [ProducesResponseType(typeof(ReceivePaymentResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> Receive(
        ReceivePaymentRequest request,
        CancellationToken cancellationToken
    )
    {
        var command = new ReceivePaymentCommand(
            new Money(request.Amount, request.Currency),
            request.Reference
        );

        var response = await _receiveHandler.Handle(
            command,
            cancellationToken
        );

        return Created($"api/finances/payments/{response.Id}", 
            response
        );
    }

    /// <summary>Voids a payment with an audit reason.</summary>
    [RequireOrganizationAccess]
    [HttpPost("{paymentId:guid}/void")]
    [ProducesResponseType(typeof(VoidPaymentResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Void(
        [FromRoute] Guid paymentId,
        [FromBody] VoidPaymentRequest request,
        CancellationToken cancellationToken
    )
    {
        var command = new VoidPaymentCommand(
            paymentId,
            request.Reason
        );

        var response = await _voidHandler.Handle(
            command,
            cancellationToken
        );

        return Ok(response);
    }
}

