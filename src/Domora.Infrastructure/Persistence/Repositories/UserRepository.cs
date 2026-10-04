using Domora.Application.Common.Persistence;
using Domora.Domain.Users;
using Domora.Domain.Users.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Domora.Infrastructure.Persistence.Repositories;

public sealed class UserRepository : IUserRepository
{
    private readonly DomoraDbContext _dbContext;

    public UserRepository(
        DomoraDbContext dbContext
    )
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(
        User user,
        CancellationToken cancellationToken
    )
    {
        await _dbContext.Users
            .AddAsync(
                user,
                cancellationToken
            );
    }

    public async Task<User?> FindByEmailAsync(
        string email,
        CancellationToken cancellationToken
    )
    {
        return await _dbContext.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(
                user => user.Email == UserEmail.Create(email),
                cancellationToken
            );
    }
}