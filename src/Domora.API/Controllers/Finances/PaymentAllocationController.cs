using Domora.API.Common.Authorization;
using Domora.API.Finances.PaymentAllocations;
using Domora.Application.Finance.Commands.AllocatePayment;
using Domora.Application.Finance.Commands.ReversePaymentAllocation;
using Domora.Domain.Common;
using Microsoft.AspNetCore.Mvc;

namespace Domora.API.Controllers.Finances;

[ApiController]
[Route("api/finances/payment-allocations")]
public sealed class PaymentAllocationsController : ControllerBase
{
    private readonly AllocatePaymentHandler _handler;
    private readonly ReversePaymentAllocationHandler _reversalHandler;

    public PaymentAllocationsController(
        AllocatePaymentHandler handler,
        ReversePaymentAllocationHandler reversalHandler
    )
    {
        _handler = handler;
        _reversalHandler = reversalHandler;
    }

    /// <summary>Allocates payment credit to invoices for a lease.</summary>
    [RequireOrganizationAccess]
    [HttpPost]
    [ProducesResponseType(typeof(AllocatePaymentResponse), StatusCodes.Status201Created)]
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

        return Created($"api/finances/payment-allocations/{response.PaymentId}", response);
    }

    /// <summary>Records a full reversal of an allocation, preserving the original allocation.</summary>
    [RequireOrganizationAccess]
    [HttpPost("{allocationId:guid}/reverse")]
    [ProducesResponseType(typeof(ReversePaymentAllocationResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Reverse(
        Guid allocationId, ReversePaymentAllocationRequest request, CancellationToken cancellationToken)
    {
        var response = await _reversalHandler.Handle(
            new ReversePaymentAllocationCommand(allocationId, request.Reason), cancellationToken);
        return Ok(response);
    }
}
