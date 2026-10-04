namespace Domora.Application.Users.Commands.RegisterUser;

public sealed record RegisterUserResponse(
    Guid UserId,
    string Email
);