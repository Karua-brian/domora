using Domora.Domain.Users;

namespace Domora.Application.Common.Persistence;

public interface IUserRepository
{
    Task AddAsync(
        User user,
        CancellationToken cancellationToken = default
    );
    
    Task<User?> FindByEmailAsync(
        string email,
        CancellationToken cancellationToken = default
    );

}