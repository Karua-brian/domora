using Domora.API.Finances.PaymentAllocations;
using Domora.Application.Finance.Commands.AllocatePayment;
using Domora.Domain.Common;
using Microsoft.AspNetCore.Mvc;

namespace Domora.API.Controllers;

[ApiController]
[Route("api/payment-allocations")]
public sealed class PaymentAllocationsController : ControllerBase
{
    private readonly AllocatePaymentHandler _handler;

    public PaymentAllocationsController(
        AllocatePaymentHandler handler
    )
    {
        _handler = handler;
    }

    [HttpPost]
    public async Task<IActionResult> Allocate(
        AllocatePaymentRequest request,
        CancellationToken cancellationToken
    )
    {
        var command = new AllocatePaymentCommand(
            request.PaymentId,
            request.InvoiceId,
            new Money(request.Amount, request.Currency)
        );

        var response = await _handler.Handle(
            command,
            cancellationToken
        );

        return Created($"api/payment-allocations/{response.Id}", response);
    }
}