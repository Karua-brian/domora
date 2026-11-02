using Domora.API.Authentication;
using Domora.Application.Common.Authentication;
using Domora.Application.Users.Commands.RegisterUser;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Domora.API.Controllers;

[ApiController]
[Route("auth")]
[AllowAnonymous]
public sealed class AuthenticationController : ControllerBase
{
    private readonly RegisterUserHandler _registerUserHandler;
    private readonly IAuthenticationService _authentication;

    public AuthenticationController(
        RegisterUserHandler registerUserHandler,
        IAuthenticationService authentication
    )
    {
        _registerUserHandler = registerUserHandler;
        _authentication = authentication;
    }

    /// <summary>Registers a user account.</summary>
    [HttpPost("register")]
    [ProducesResponseType(typeof(RegisterUserResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<RegisterUserResponse>> Register(
        RegisterRequest request,
        CancellationToken cancellationToken
    )
    {
        var command = new RegisterUserCommand(
            request.Email,
            request.Password
        );

        var response = await _registerUserHandler.Handle(
            command,
            cancellationToken
        );

        return Created(
            $"/api/users/{response.UserId}",
            response
        );
    }

    /// <summary>Authenticates a user and returns a JWT access token.</summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status401Unauthorized)]
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
