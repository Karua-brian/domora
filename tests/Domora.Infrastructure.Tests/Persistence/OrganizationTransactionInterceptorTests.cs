using Domora.Application.Common.Context;
using Domora.Infrastructure.Persistence;
using Domora.Infrastructure.Persistence.Interceptors;
using Microsoft.EntityFrameworkCore;

namespace Domora.Infrastructure.Tests.Persistence;

public sealed class OrganizationTransactionInterceptorTests
{
    private readonly string _connectionString;

    public OrganizationTransactionInterceptorTests()
    {
        _connectionString = 
        Environment.GetEnvironmentVariable("DomoraTest")
        ?? throw new InvalidOperationException(
            "DomoraTest connection string is not configured.");
    }

    private sealed class TestOrganizationContext : IOrganizationContext
    {
        public TestOrganizationContext(Guid organizationId)
        {
            OrganizationId = organizationId;
        }

        public Guid OrganizationId { get; }

    }
    [Fact]
    public async Task Each_transaction_should_receive_its_organization_context()
    {
        var organizationA = Guid.NewGuid();
        var organizationB = Guid.NewGuid();

        // Transaction A
        var contextA = new TestOrganizationContext(organizationA);

        var interceptorA =
            new OrganizationTransactionInterceptor(contextA);

        var optionsA =
            new DbContextOptionsBuilder<DomoraDbContext>()
                .UseNpgsql(_connectionString)
                .AddInterceptors(interceptorA)
                .Options;

        await using var dbA =
            new DomoraDbContext(optionsA);

        var unitOfWorkA =
            new UnitOfWork(dbA, contextA);

        await using var transactionA =
            await unitOfWorkA.BeginTransactionAsync(
                CancellationToken.None);

        var currentA =
            await dbA.Database
                .SqlQuery<string>(
                    $"""
                    SELECT current_setting(
                        'app.organization_id',
                        true
                    ) AS "Value"
                    """
                )
                .SingleAsync();

        Assert.Equal(
            organizationA.ToString(),
            currentA
        );

        await transactionA.RollbackAsync();

        // Transaction B
        var contextB = new TestOrganizationContext(organizationB);

        var interceptorB =
            new OrganizationTransactionInterceptor(contextB);

        var optionsB =
            new DbContextOptionsBuilder<DomoraDbContext>()
                .UseNpgsql(_connectionString)
                .AddInterceptors(interceptorB)
                .Options;

        await using var dbB =
            new DomoraDbContext(optionsB);

        var unitOfWorkB =
            new UnitOfWork(dbB, contextB);

        await using var transactionB =
            await unitOfWorkB.BeginTransactionAsync(
                CancellationToken.None);

        var currentB =
            await dbB.Database
                .SqlQuery<string>(
                    $"""
                    SELECT current_setting(
                        'app.organization_id',
                        true
                    ) AS "Value"
                    """
                )
                .SingleAsync();

        Assert.Equal(
            organizationB.ToString(),
            currentB
        );

        await transactionB.RollbackAsync();
    }    

    [Fact]
    public async Task Transaction_should_set_organization_context_for_rls()
    {
        var organizationContext = new TestOrganizationContext(Guid.NewGuid());

        var interceptor = new OrganizationTransactionInterceptor(organizationContext);

        var options = new DbContextOptionsBuilder<DomoraDbContext>()
            .UseNpgsql(_connectionString)
            .AddInterceptors(interceptor)
            .Options;

        await using var context = new DomoraDbContext(options);

        var unitOfWork = new UnitOfWork(context, organizationContext);

        await using var transaction = await unitOfWork.BeginTransactionAsync(CancellationToken.None);
        
        var currentOrganizationId = await context.Database
            .SqlQuery<string>(
                $"""
                SELECT current_setting(
                    'app.organization_id', 
                    true
                ) AS "Value"
                """
            )
            .SingleAsync();
        
        Assert.Equal(
            organizationContext.OrganizationId.ToString(),
            currentOrganizationId
        );

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task Organization_context_should_not_survive_transaction()
    {
        var organizationId = Guid.NewGuid();

        var organizationContext =
            new TestOrganizationContext(organizationId);

        var interceptor =
            new OrganizationTransactionInterceptor(
                organizationContext);

        var options =
            new DbContextOptionsBuilder<DomoraDbContext>()
                .UseNpgsql(_connectionString)
                .AddInterceptors(interceptor)
                .Options;

        await using var context =
            new DomoraDbContext(options);

        var unitOfWork =
            new UnitOfWork(
                context,
                organizationContext
            );

        await using (var transaction =
            await unitOfWork.BeginTransactionAsync(
                CancellationToken.None))
        {
            var currentOrganizationId =
                await context.Database
                    .SqlQuery<string>(
                        $"""
                        SELECT current_setting(
                            'app.organization_id',
                            true
                        ) AS "Value"
                        """
                    )
                    .SingleAsync();

            Assert.Equal(
                organizationId.ToString(),
                currentOrganizationId
            );

            await transaction.RollbackAsync();
        }

        var valueAfterTransaction =
            await context.Database
                .SqlQuery<string?>(
                    $"""
                    SELECT current_setting(
                        'app.organization_id',
                        true
                    ) AS "Value"
                    """
                )
                .SingleOrDefaultAsync();

        Assert.Null(valueAfterTransaction);
    }
}