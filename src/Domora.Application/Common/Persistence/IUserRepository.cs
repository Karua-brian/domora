using Domora.Domain.Users;

namespace Domora.Application.Common.Persistence;

public interface IUserRepository
{
    Task<User?> FindByEmailAsync(
        string email,
        CancellationToken cancellationToken = default
    );
}