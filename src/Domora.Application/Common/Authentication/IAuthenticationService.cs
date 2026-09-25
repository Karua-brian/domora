namespace Domora.Application.Common.Authentication;

public interface IAuthenticationService
{
    Task<AuthenticationResult?> AuthenticateAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default
    );
}