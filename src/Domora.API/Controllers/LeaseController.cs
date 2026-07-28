using Domora.API.Leases;
using Domora.Application.Leasing.Commands.RegisterLease;
using Domora.Domain.Common;
using Microsoft.AspNetCore.Mvc;

namespace Domora.API.Controllers;

[ApiController]
[Route("api/leases")]

public sealed class LeaseController : ControllerBase
{
    private readonly RegisterLeaseHandler _handler;

    public LeaseController(RegisterLeaseHandler handler)
    {
        _handler = handler;
    }

    [HttpPost]      
    public async Task<IActionResult> Register(RegisterLeaseRequest request, CancellationToken cancellationToken)
    {
        var command = new RegisterLeaseCommand(
            request.UnitId,
            request.TenantId,
            request.StartDate,
            new Money(request.MonthlyRent, request.Currency)
        );

        var response = await _handler.Handle(command, cancellationToken);

        return Created($"/api/leases/{response.Id}", response);
    }

}