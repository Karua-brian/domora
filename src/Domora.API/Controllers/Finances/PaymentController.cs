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

    [RequireOrganizationAccess]
    [HttpPost]
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

    [RequireOrganizationAccess]
    [HttpPost("{paymentId:guid}/void")]
    public async Task<IActionResult> Void(
        [FromRoute] Guid paymentId,
        CancellationToken cancellationToken
    )
    {
        var command = new VoidPaymentCommand(
            paymentId
        );

        var response = await _voidHandler.Handle(
            command,
            cancellationToken
        );

        return Ok(response);
    }
}

