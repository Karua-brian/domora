namespace Domora.Application.Common.Persistence;

public interface IUnitOfWork
{
    Task<ITransaction> BeginTransactionAsync(
        CancellationToken cancellationToken = default
    );
    Task SaveChangesAsync(
        CancellationToken cancellationToken = default
    );
}

public interface ITransaction : IAsyncDisposable
{
    Task CommitAsync(
        CancellationToken cancellationToken = default
    );

    Task RollbackAsync(
        CancellationToken cancellationToken = default
    );
}
