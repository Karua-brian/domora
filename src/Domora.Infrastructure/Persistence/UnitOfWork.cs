using Domora.Application.Common.Exceptions;
using Domora.Application.Common.Persistence;
using Domora.Application.Common.Context;
using Domora.Domain.Common.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;

namespace Domora.Infrastructure.Persistence;

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly DomoraDbContext _dbContext;
    private readonly IOrganizationContext _organizationContext;

    public UnitOfWork(
        DomoraDbContext dbContext,
        IOrganizationContext organizationContext
    )
    {
        _dbContext = dbContext;
        _organizationContext = organizationContext;
    }
    
    public async Task SaveChangesAsync(
        CancellationToken cancellationToken
    )
    {
        try
        {
            // Reuse an existing transaction if the caller already owns one.
            // Otherwise, create an explicit transaction so the RLS interceptor
            // can apply the organization context before database writes.
            if (_dbContext.Database.CurrentTransaction is null)
            {
                await using var transaction =
                    await _dbContext.Database.BeginTransactionAsync(
                        cancellationToken);

                await _dbContext.SaveChangesAsync(cancellationToken);

                await transaction.CommitAsync(cancellationToken);
            }
            else
            {
                // The existing transaction already passed through the
                // transaction-start interceptor.
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new ConcurrencyException(
                "The resource was modified by another operation. Please reload and try again.",
                ex);
        }
        catch (DbUpdateException ex)
            when (TryGetPostgresException(ex, out var postgresException)
                && postgresException.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            throw MapUniqueConstraintViolation(postgresException, ex);
        }
    }


    private static bool TryGetPostgresException(
        DbUpdateException exception,
        out PostgresException postgresException
    )
    {
        postgresException = exception.InnerException as PostgresException
            ?? exception.GetBaseException() as PostgresException
            ?? null!;

        return postgresException is not null;
    }

    private static ResourceConflictException MapUniqueConstraintViolation(
        PostgresException postgresException,
        DbUpdateException exception
    )
    {
        var message = postgresException.ConstraintName switch
        {
            "IX_Leases_UnitId" =>
                "An active lease already exists for this unit.",
                
            "UX_Invoices_LeaseId_Type_DueDate" =>
                "An invoice of this type has already been generated for this lease period.",

            "IX_Users_Email" =>
                "A user with this email already exists.",

            "UX_Organizations_Name" =>
                "An organization with this name already exists.",

            "UX_Properties_OrganizationId_Name" =>
                "A property with this name already exists in the organization.",

            "UX_Units_PropertyId_Number" =>
                "A unit with this number already exists in the property.",

            "UX_Payments_OrganizationId_Reference" =>
                "A payment with this reference already exists in the organization.",

            _ =>
                $"A record conflicts with an existing unique constraint ({postgresException.ConstraintName ?? "unknown"})."
        };

        return new ResourceConflictException(message, exception);
    }

    public async Task<ITransaction> BeginTransactionAsync(
        CancellationToken cancellationToken
    )
    {
        var organizationId = _organizationContext.OrganizationId;

        if (organizationId == Guid.Empty)
        {
            throw new InvalidOperationException(
                "Organization context is unavailable."
            );
        }

        var transaction = await _dbContext.Database
            .BeginTransactionAsync(cancellationToken);

        return new EfCoreTransaction(transaction);
    }
}

public sealed class EfCoreTransaction(
    IDbContextTransaction transaction
) : ITransaction
{
    private readonly IDbContextTransaction _transaction = transaction;

    public async Task CommitAsync(
        CancellationToken cancellationToken
    )
    {
        await _transaction.CommitAsync(cancellationToken);
    }

    public async Task RollbackAsync(
        CancellationToken cancellationToken
    )
    {
        await _transaction.RollbackAsync(cancellationToken);
    }

    public ValueTask DisposeAsync()
    {
        return _transaction.DisposeAsync();
    }
}