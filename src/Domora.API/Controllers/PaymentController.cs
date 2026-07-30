using Domora.API.Finances.Payments;
using Domora.Application.Finance.Commands.ReceivePayment;
using Domora.Domain.Common;
using Microsoft.AspNetCore.Mvc;

namespace Domora.API.Controllers;

[ApiController]
[Route("api/payments")]

public sealed class PaymentController : ControllerBase
{
    private readonly ReceivePaymentHandler _handler;

    public PaymentController(
        ReceivePaymentHandler handler
    )
    {
        _handler = handler;
    }

    [HttpPost]
    public async Task<IActionResult> Receive(
        ReceivePaymentRequest request,
        CancellationToken cancellationToken
    )
    {
        var command = new ReceivePaymentCommand(
            new Money(request.Amount, request.Currency),
            request.PaidAt.ToUniversalTime(),
            request.Reference
        );

        var response = await _handler.Handle(
            command,
            cancellationToken
        );

        return Created($"api/payments/{response.Id}", response);
    }
}

