using Domora.API.Propertys;
using Domora.Application.Properties.Commands.RegisterProperty;
using Domora.Application.Properties.Queries.GetProperty;
using Microsoft.AspNetCore.Mvc;

namespace Domora.API.Controllers;

[ApiController]
[Route("api/properties")]
public sealed class PropertyController : ControllerBase
{
    private readonly RegisterPropertyHandler _registerPropertyHandler;

    private readonly GetPropertyHandler _getPropertyHandler;

    public PropertyController(
        RegisterPropertyHandler registerPropertyHandler,
        GetPropertyHandler getPropertyHandler
    )
    {
        _registerPropertyHandler = registerPropertyHandler;
        _getPropertyHandler = getPropertyHandler;
    }

    [HttpPost]
    public async Task<IActionResult> Register(
        RegisterPropertyRequest request, 
        CancellationToken cancellationToken
        )
    {

        var command = new RegisterPropertyCommand(request.Name);

        var response = await _registerPropertyHandler.Handle(
            command,
            cancellationToken
        );

        return Created($"/api/properties/{response.Id}" ,response);
    }

    [HttpGet("{propertyId:guid}")]
    public async Task<IActionResult> Get(
        Guid propertyId,
        CancellationToken cancellationToken
    )
    {
        var response = await _getPropertyHandler.Handle(
            new GetPropertyQuery(propertyId),
            cancellationToken
        );

        return Ok(response);
    }
}