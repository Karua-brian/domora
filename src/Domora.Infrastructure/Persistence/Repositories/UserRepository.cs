using Domora.Application.Common.Persistence;
using Domora.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace Domora.Infrastructure.Persistence.Repositories;

public sealed class UserRepository : IUserRepository
{
    private DomoraDbContext _dbContext;

    public UserRepository(
        DomoraDbContext dbContext
    )
    {
        _dbContext = dbContext;
    }

    public async Task<User?> FindByEmailAsync(
        string email,
        CancellationToken cancellationToken
    )
    {
        return await _dbContext.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(
                user => user.Email == email,
                cancellationToken
            );
    }
}