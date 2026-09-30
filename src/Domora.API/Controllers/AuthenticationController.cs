using Domora.API.Authentication;
using Domora.Application.Common.Authentication;
using Microsoft.AspNetCore.Mvc;

namespace Domora.API.Controllers;

[ApiController]
[Route("auth")]
public sealed class AuthenticationController : ControllerBase
{
    private readonly IAuthenticationService _authentication;

    public AuthenticationController(
        IAuthenticationService authentication
    )
    {
        _authentication = authentication;
    }

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login(
        LoginRequest request,
        CancellationToken cancellationToken
    )
    {
        var result = await _authentication.AuthenticateAsync(
            request.Email,
            request.Password,
            cancellationToken
        );

        if (result is null)
            return Unauthorized(
                "Invalid email or password."
            );

        return Ok(
            new LoginResponse(
                result.UserId,
                result.AccessToken
            )
        );
    }
}