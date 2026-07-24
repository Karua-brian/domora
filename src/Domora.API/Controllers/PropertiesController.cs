using Domora.API.Propertys;
using Domora.Application.Properties.Commands.RegisterProperty;
using Microsoft.AspNetCore.Mvc;

namespace Domora.API.Controllers;

[ApiController]
[Route("api/properties")]
public sealed class PropertyController : ControllerBase
{
    public readonly RegisterPropertyHandler _handler;

    public PropertyController(RegisterPropertyHandler handler)
    {
        _handler = handler;
    }

    [HttpPost]
    public async Task<IActionResult> Register(RegisterPropertyRequest request, CancellationToken cancellationToken)
    {
        Console.WriteLine($"Registering property with name: {request.Name}");

        var command = new RegisterPropertyCommand(request.OrganizationId, request.Name);

        var response = await _handler.Handle(command, cancellationToken);

        return Created($"api/properties/{response.Id}" ,response);
    }
}