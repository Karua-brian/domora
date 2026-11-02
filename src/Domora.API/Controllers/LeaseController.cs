using Domora.API.Common.Authorization;
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

    /// <summary>Registers a lease for a unit in the current organization.</summary>
    [RequireOrganizationAccess]
    [HttpPost]
    [ProducesResponseType(typeof(RegisterLeaseResponse), StatusCodes.Status201Created)]
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

    /// <summary>Ends an active lease in the current organization.</summary>
    [RequireOrganizationAccess]
    [HttpPost("{leaseId:guid}/end")]
    [ProducesResponseType(typeof(EndLeaseResponse), StatusCodes.Status200OK)]
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
