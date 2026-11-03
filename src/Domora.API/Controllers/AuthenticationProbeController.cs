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

    /// <summary>Returns the authenticated user's identifier.</summary>
    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
    public ActionResult<Guid> Me()
    {
        return Ok(
            _userContext.UserId
        );
    }
}
