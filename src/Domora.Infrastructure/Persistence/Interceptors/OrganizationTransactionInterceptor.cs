using System.Data.Common;
using Domora.Application.Common.Context;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Npgsql;

namespace Domora.Infrastructure.Persistence.Interceptors;

public sealed class OrganizationTransactionInterceptor : DbTransactionInterceptor
{
    private readonly IOrganizationContext _organizationContext;

    public OrganizationTransactionInterceptor(
        IOrganizationContext organizationContext
    )
    {
        _organizationContext = organizationContext;
    }

    public override async ValueTask<DbTransaction> TransactionStartedAsync(
        DbConnection connection,
        TransactionEndEventData eventData,
        DbTransaction result,
        CancellationToken cancellationToken = default
    )
    {
        var organizationId = _organizationContext.OrganizationId;

        if (organizationId == Guid.Empty)
            throw new InvalidOperationException(
                "Organization context is unavailable."
            );

        if (connection is not NpgsqlConnection npgsqlConnection)
            throw new InvalidOperationException(
                "Organization RLS requires an Npgsql connection."
            );

        if (result is not NpgsqlTransaction npgsqlTransaction)
            throw new InvalidOperationException(
                "Active transaction must be an NpgsqlTransaction."
            );

        await using var command = npgsqlConnection.CreateCommand();

        command.Transaction = npgsqlTransaction;

        command.CommandText = """
            SELECT set_config(
                'app.organization_id',
                @organization_id,
                true
            );
        """;

        command.Parameters.AddWithValue(
            "organization_id",
            organizationId.ToString()
        );

        await command.ExecuteNonQueryAsync(cancellationToken);

        return await base.TransactionStartedAsync(
            connection,
            eventData,
            result,
            cancellationToken
        );
    }
}