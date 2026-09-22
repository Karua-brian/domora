using Domora.Application.Common.Context;
using Domora.Domain.Organizations;
using Domora.Domain.Organizations.ValueObjects;
using Domora.Domain.Properties;
using Domora.Domain.Properties.ValueObjects;
using Domora.Infrastructure.Persistence;
using Domora.Infrastructure.Persistence.Interceptors;
using Microsoft.EntityFrameworkCore;

namespace Domora.Infrastructure.Tests.Persistence;

public sealed class OrganizationTransactionInterceptorTests
{
    private readonly string _connectionString;

    private readonly DbContextOptions<DomoraDbContext> _options;

    public OrganizationTransactionInterceptorTests()
    {
        _connectionString = 
        Environment.GetEnvironmentVariable("DomoraTest")
        ?? throw new InvalidOperationException(
            "DomoraTest connection string is not configured.");
        
        _options = new DbContextOptionsBuilder<DomoraDbContext>()
            .UseNpgsql(_connectionString)
            .Options;
    }

    private sealed class TestOrganizationContext : IOrganizationContext
    {
        public TestOrganizationContext(Guid organizationId)
        {
            OrganizationId = organizationId;
        }

        public Guid OrganizationId { get; }

    }

    private async Task ExecuteAsOrganizationContextAsync(
        Guid organizationId,
        Func<DomoraDbContext, Task> action
    )
    {
        var organizationContext = new TestOrganizationContext(organizationId);

        var interceptor = new OrganizationTransactionInterceptor(
            organizationContext
        );

        var options = new DbContextOptionsBuilder<DomoraDbContext>()
            .UseNpgsql(_connectionString)
            .AddInterceptors(interceptor)
            .Options;

        await using var context = new DomoraDbContext(options);

        await using var transaction =
            await context.Database.BeginTransactionAsync();

        await action(context);

        await transaction.CommitAsync();
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
    public async Task Each_transaction_should_receive_its_own_organization_context()
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

        Assert.NotEqual(
            organizationId.ToString(),
            valueAfterTransaction
        );

        Assert.True(
            string.IsNullOrEmpty(valueAfterTransaction)
        );
        
    }

    [Fact]
    public async Task Organization_should_not_see_another_organizations_properties()
    {
        var organizationA = Organization.Register(
            OrganizationName.Create($"Organization A {Guid.NewGuid():N}")
        );

        var organizationB = Organization.Register(
            OrganizationName.Create($"Organization B {Guid.NewGuid():N}")
        );

        var propertyA = Property.Register(
            organizationA.Id,
            PropertyName.Create($"Property A {Guid.NewGuid():N}")
        );

        var propertyB = Property.Register(
            organizationB.Id,
            PropertyName.Create($"Property B {Guid.NewGuid():N}")
        );

        await ExecuteAsOrganizationContextAsync(
            organizationA.Id,
            async context =>
            {
                await context.Organizations.AddAsync(organizationA);
                await context.Properties.AddAsync(propertyA);
                await context.SaveChangesAsync();
            }
        );

        await ExecuteAsOrganizationContextAsync(
            organizationB.Id,
            async context =>
            {
                await context.Organizations.AddAsync(organizationB);
                await context.Properties.AddAsync(propertyB);
                await context.SaveChangesAsync();
            }
        );

        var organizationAContext =
            new TestOrganizationContext(organizationA.Id);

        var interceptor =
            new OrganizationTransactionInterceptor(
                organizationAContext
            );

        var options =
            new DbContextOptionsBuilder<DomoraDbContext>()
                .UseNpgsql(_connectionString)
                .AddInterceptors(interceptor)
                .Options;

        await using var context =
            new DomoraDbContext(options);

        await using var transaction =
            await context.Database.BeginTransactionAsync();

        var visibleProperties =
            await context.Properties
                .AsNoTracking()
                .ToListAsync();

        Assert.Single(visibleProperties);

        Assert.Equal(
            propertyA.Id,
            visibleProperties[0].Id
        );

        Assert.DoesNotContain(
            visibleProperties,
            property => property.Id == propertyB.Id
        );

        await transaction.RollbackAsync();
     }
}
