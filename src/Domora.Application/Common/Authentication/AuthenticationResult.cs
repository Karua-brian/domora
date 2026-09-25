namespace Domora.Application.Common.Authentication;

public sealed record AuthenticationResult(
    Guid UserId,
    string AccessToken
);