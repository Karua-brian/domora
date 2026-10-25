using System.Data.Common;
using Domora.Application.Common.Context;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Domora.Infrastructure.Persistence.Interceptors;

public sealed class OrganizationTransactionInterceptor : DbTransactionInterceptor
{
    private readonly IOrganizationContext _organizationContext;
    // private readonly ILogger<OrganizationTransactionInterceptor> _logger;


    public OrganizationTransactionInterceptor(
        IOrganizationContext organizationContext
        // ILogger<OrganizationTransactionInterceptor> logger
    )
    {
        _organizationContext = organizationContext;
        // _logger = logger;
    }

    public override async ValueTask<DbTransaction> TransactionStartedAsync(
        DbConnection connection,
        TransactionEndEventData eventData,
        DbTransaction result,
        CancellationToken cancellationToken = default
    )
    {
        var organizationId = _organizationContext.OrganizationId;

        // _logger.LogInformation(
        //     "RLS transaction started. OrganizationId: {OrganizationId}",
        //     organizationId
        // );

        if (organizationId == Guid.Empty)
        {
            return result;
        }

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

        await using var verifyCommand = npgsqlConnection.CreateCommand();
            verifyCommand.Transaction = npgsqlTransaction;
            verifyCommand.CommandText =
                "SELECT current_setting('app.organization_id', true)";

        var activeOrganizationId =
                await verifyCommand.ExecuteScalarAsync(cancellationToken);

        // _logger.LogInformation(
        //     "Organization context inside transaction: {OrganizationId}",
        //     activeOrganizationId
        // );

        return await base.TransactionStartedAsync(
            connection,
            eventData,
            result,
            cancellationToken
        );
    }
}