using Domora.Application.Common.Persistence;

namespace Domora.Application.Common.Authentication;

public sealed class AuthenticationService : IAuthenticationService
{
    private readonly IUserRepository _users;

    private readonly IPasswordHasher _passwordHasher;

    private readonly IAccessTokenGenerator _accessTokenGenerator;

    public AuthenticationService(
        IUserRepository users,
        IPasswordHasher passwordHasher,
        IAccessTokenGenerator accessTokenGenerator
    )
    {
        _users = users;
        _passwordHasher = passwordHasher;
        _accessTokenGenerator = accessTokenGenerator;
    }

    public async Task<AuthenticationResult?> AuthenticateAsync(
        string email,
        string password,
        CancellationToken cancellationToken
    )
    {
        var user = await _users.FindByEmailAsync(
            email,
            cancellationToken
        );

        if (user is null)
            return null;

        if (!_passwordHasher.Verify(
                password,
                user.PasswordHash.Value
        ))
        {
            return null;
        }

        return new AuthenticationResult(
            user.Id,
            _accessTokenGenerator.Generate(user.Id)     
        );
    }
}