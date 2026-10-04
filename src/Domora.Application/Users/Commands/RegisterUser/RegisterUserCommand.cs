namespace Domora.Application.Users.Commands.RegisterUser;

public sealed record RegisterUserCommand(
    string Email,
    string Password
);