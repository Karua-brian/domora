using Microsoft.AspNetCore.Mvc;
using Domora.Application.Organizations.Commands.RegisterOrganization;
using Domora.API.Organizations;

namespace Domora.API.Controllers;

[ApiController]
[Route("api/Organizations")]
public sealed class OrganizationsController : ControllerBase
{
    private readonly RegisterOrganizationHandler _handler;

    public OrganizationsController(RegisterOrganizationHandler handler)
    {
        _handler = handler;
    }

    [HttpPost]
    public async Task<IActionResult> Register(RegisterOrganizationRequest request, CancellationToken cancellationToken)
    {
        Console.WriteLine($"Request ID: {Guid.NewGuid()}");

        Console.WriteLine($"Registering organization with name: {request.Name}");

        var command = new RegisterOrganizationCommand(request.Name);

        var response = await _handler.Handle(command, cancellationToken);

        return Created($"/api/organizations/{response.Id}", response);
    }
}
