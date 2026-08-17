using Domora.API.Units;
using Domora.Application.Units.Commands.RegisterUnit;
using Microsoft.AspNetCore.Mvc;

namespace Domora.API.Controllers;

[ApiController]
[Route("api/units")]
public sealed class UnitController : ControllerBase
{
    public readonly RegisterUnitHandler _handler;

    public UnitController(RegisterUnitHandler handler)
    {
        _handler = handler;
    }

    [HttpPost]
    public async Task<IActionResult> Register(
        RegisterUnitRequest request, 
        CancellationToken cancellationToken)
    {
        var command = new RegisterUnitCommand(
            request.PropertyId, 
            request.Number, 
            request.Type
            );

        var response = await _handler.Handle(command, cancellationToken);

        return Created($"/api/units/{response.Id}", response);

    }
}