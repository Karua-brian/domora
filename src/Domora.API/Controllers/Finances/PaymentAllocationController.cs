using Domora.API.Common.Authorization;
using Domora.API.Finances.PaymentAllocations;
using Domora.Application.Finance.Commands.AllocatePayment;
using Domora.Domain.Common;
using Microsoft.AspNetCore.Mvc;

namespace Domora.API.Controllers.Finances;

[ApiController]
[Route("api/finances/payment-allocations")]
public sealed class PaymentAllocationsController : ControllerBase
{
    private readonly AllocatePaymentHandler _handler;

    public PaymentAllocationsController(
        AllocatePaymentHandler handler
    )
    {
        _handler = handler;
    }

    [RequireOrganizationAccess]
    [HttpPost]
    public async Task<IActionResult> Allocate(
        AllocatePaymentRequest request,
        CancellationToken cancellationToken
    )
    {
        var command = new AllocatePaymentCommand(
            request.PaymentId,
            request.LeaseId,
            new Money(request.Amount, request.Currency)
        );

        var response = await _handler.Handle(
            command,
            cancellationToken
        );

        return Created($"api/finances/payment-allocations/{response.Id}", response);
    }
}