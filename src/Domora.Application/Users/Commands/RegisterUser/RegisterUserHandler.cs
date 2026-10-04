using Domora.Application.Common.Authentication;
using Domora.Application.Common.Persistence;
using Domora.Domain.Users;
using Domora.Domain.Users.ValueObjects;

namespace Domora.Application.Users.Commands.RegisterUser;

public sealed class RegisterUserHandler
{
    private readonly IUserRepository _users;

    private readonly IPasswordHasher _passwordHasher;

    private readonly IUnitOfWork _unitOfWork;

    public RegisterUserHandler(
        IUserRepository userS,
        IPasswordHasher passwordHasher,
        IUnitOfWork unitOfWork
    )
    {
        _users = userS;
        _passwordHasher = passwordHasher;
        _unitOfWork = unitOfWork;
    }

    public async Task<RegisterUserResponse> Handle(
        RegisterUserCommand command,
        CancellationToken cancellationToken
    )
    {
        var userEmail = UserEmail.Create(
            command.Email
        );

        var userPassword = UserPassword.Create(
            command.Password
        );

        var passwordHash = 
            _passwordHasher.Hash(
                userPassword.Value
            );

        var secureUserPassword = UserPassword.FromHash(passwordHash);

        var user = User.Register(
            userEmail,
            secureUserPassword
        );
        
        await _users.AddAsync(
            user,
            cancellationToken
        );

        await _unitOfWork.SaveChangesAsync(
            cancellationToken
        );

        return new RegisterUserResponse(
            user.Id,
            user.Email.Value
        );
    }
}