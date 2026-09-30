namespace Domora.API.Authentication;

public sealed record LoginResponse(
    Guid UserId,
    string AccessToken
);
