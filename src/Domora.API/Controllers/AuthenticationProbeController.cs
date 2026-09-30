using Domora.Application.Common.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Domora.API.Authentication;

[ApiController]
[Route("auth")]
public sealed class AuthenticationProbeController : ControllerBase
{
    private readonly IUserContext _userContext;

    public AuthenticationProbeController(
        IUserContext userContext
    )
    {
        _userContext = userContext;
    }

    [Authorize]
    [HttpGet("me")]
    public ActionResult<Guid> Me()
    {
        return Ok(
            _userContext.UserId
        );
    }
}