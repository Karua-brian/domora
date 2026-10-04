namespace Domora.API.Authentication;

public sealed record RegisterRequest(
    string Email,
    string Password
);