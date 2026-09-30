namespace Domora.API.Authentication;

public sealed record LoginRequest(
    string Email,
    string Password
);