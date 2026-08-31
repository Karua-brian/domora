using Domora.API.Leases;
using Domora.Application.Leasing.Commands.EndLease;
using Domora.Application.Leasing.Commands.RegisterLease;
using Domora.Domain.Common;
using Microsoft.AspNetCore.Mvc;

namespace Domora.API.Controllers;

[ApiController]
[Route("api/leases")]

public sealed class LeaseController : ControllerBase
{
    private readonly RegisterLeaseHandler _registerLeaseHandler;

    private readonly EndLeaseHandler _endLeaseHandler;

    public LeaseController(
        RegisterLeaseHandler registerLeaseHandler,
        EndLeaseHandler endLeaseHandler
        )
    {
        _registerLeaseHandler = registerLeaseHandler;
        _endLeaseHandler = endLeaseHandler;
    }

    [HttpPost]      
    public async Task<IActionResult> Register(
        RegisterLeaseRequest request,
        CancellationToken cancellationToken)
    {
        var command = new RegisterLeaseCommand(
            request.UnitId,
            request.TenantId,
            new Money(
                request.MonthlyRent, 
                request.Currency  
            )

        );

        var response = await _registerLeaseHandler
            .Handle(command, cancellationToken);

        return Created($"/api/leases/{response.Id}", response);
    }

    [HttpPost("{leaseId:guid}/end")]
    public async Task<IActionResult> End(
        Guid leaseId,
        EndLeaseRequest request,
        CancellationToken cancellationToken
    )
    {   
        var command = new EndLeaseCommand(
            leaseId,
            request.EndDate
        );

        var response = await _endLeaseHandler
            .Handle(command, cancellationToken);

        return Ok(response);
    }

}