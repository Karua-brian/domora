using Domora.Application.Common.Context;
using Domora.Domain.Organizations;
using Domora.Domain.Organizations.ValueObjects;
using Domora.Domain.Properties;
using Domora.Domain.Properties.ValueObjects;
using Domora.Infrastructure.Persistence;
using Domora.Infrastructure.Persistence.Interceptors;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Domorsa.Infrastructure.Tests.Security;

public sealed class OrganizationIsolationTests
{
    private readonly string _connectionString;

    public OrganizationIsolationTests()
    {
        _connectionString = Environment.GetEnvironmentVariable(
            "DomoraTest"
        )!;

        if (string.IsNullOrWhiteSpace(_connectionString))    
            throw new InvalidOperationException(
                "DomoraTest connection is not configured"
            );
    }

    public sealed class TestOrganizationContext : IOrganizationContext
    {
        public TestOrganizationContext(Guid organizationId)
        {
            OrganizationId = organizationId;
        }

        public Guid OrganizationId { get; }
    }

    private DomoraDbContext ExecuteAsOrganizationContextAsync(
        Guid organizationId
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

        return new DomoraDbContext(options);
    }

    [Fact]
    public async Task Organization_should_not_see_another_organizations_property()
    {
        // Arrange
        var organizationA =
            Organization.Register(
                OrganizationName.Create(
                    $"Organization A {Guid.NewGuid():N}"
                )
            );

        var organizationB =
            Organization.Register(
                OrganizationName.Create(
                    $"Organization B {Guid.NewGuid():N}"
                )
            );

        Guid propertyBId;

        // Create Organization A and its property.
        await using (var context =
            ExecuteAsOrganizationContextAsync(organizationA.Id))
        {
            await using var transaction =
                await context.Database.BeginTransactionAsync();

            await context.Organizations.AddAsync(
                organizationA
            );

            var propertyA =
                Property.Register(
                    organizationA.Id,
                    PropertyName.Create(
                        $"Property A {Guid.NewGuid():N}"
                    )
                );

            await context.Properties.AddAsync(propertyA);

            await context.SaveChangesAsync();
            await transaction.CommitAsync();
        }

        // Create Organization B and its property.
        await using (var context =
            ExecuteAsOrganizationContextAsync(organizationB.Id))
        {
            await using var transaction =
                await context.Database.BeginTransactionAsync();

            await context.Organizations.AddAsync(
                organizationB
            );

            var propertyB =
                Property.Register(
                    organizationB.Id,
                    PropertyName.Create(
                        $"Property B {Guid.NewGuid():N}"
                    )
                );

            await context.Properties.AddAsync(propertyB);

            await context.SaveChangesAsync();

            propertyBId = propertyB.Id;

            await transaction.CommitAsync();
        }

        // Act
        await using var organizationAContext =
            ExecuteAsOrganizationContextAsync(organizationA.Id);

        await using var organizationATransaction =
            await organizationAContext.Database
                .BeginTransactionAsync();

        var property =
            await organizationAContext.Properties
                .SingleOrDefaultAsync(
                    p => p.Id == propertyBId
                );

        // Assert
        Assert.Null(property);

        await organizationATransaction.RollbackAsync();
    }

    [Fact]
    public async Task Organization_should_not_insert_property_for_another_organization()
    {
        // Arrange
        var organizationA =
            Organization.Register(
                OrganizationName.Create(
                    $"Organization A {Guid.NewGuid():N}"
                )
            );

        var organizationB =
            Organization.Register(
                OrganizationName.Create(
                    $"Organization B {Guid.NewGuid():N}"
                )
            );

        // Create both organizations.
        await using (var context =
            ExecuteAsOrganizationContextAsync(organizationA.Id))
        {
            await using var transaction =
                await context.Database.BeginTransactionAsync();

            await context.Organizations.AddAsync(
                organizationA
            );

            await context.SaveChangesAsync();
            await transaction.CommitAsync();
        }

        await using (var context =
            ExecuteAsOrganizationContextAsync(organizationB.Id))
        {
            await using var transaction =
                await context.Database.BeginTransactionAsync();

            await context.Organizations.AddAsync(
                organizationB
            );

            await context.SaveChangesAsync();
            await transaction.CommitAsync();
        }

        // Act
        await using var organizationAContext =
            ExecuteAsOrganizationContextAsync(organizationA.Id);

        await using var organizationATransaction =
            await organizationAContext.Database
                .BeginTransactionAsync();

        var propertyOwnedByB =
            Property.Register(
                organizationB.Id,
                PropertyName.Create(
                    $"Property B {Guid.NewGuid():N}"
                )
            );

        await organizationAContext.Properties.AddAsync(
            propertyOwnedByB
        );

        // Assert
        var exception =
            await Assert.ThrowsAsync<DbUpdateException>(
                () => organizationAContext.SaveChangesAsync()
            );

        Assert.IsType<PostgresException>(
            exception.InnerException
        );

        var postgresException =
            (PostgresException)exception.InnerException!;

        Assert.Equal(
            PostgresErrorCodes.InsufficientPrivilege,
            postgresException.SqlState
        );

        await organizationATransaction.RollbackAsync();
    }

    [Fact]
    public async Task Organization_should_not_update_property_for_another_organization()
    {
        // Arrange
        var organizationA =
            Organization.Register(
                OrganizationName.Create(
                    $"Organization A {Guid.NewGuid():N}"
                )
            );
        
        var organizationB = 
            Organization.Register(
                OrganizationName.Create(
                    $"Organization B {Guid.NewGuid():N}"
                )
            );
        
        Guid propertyBId;

        // Create Organization A.
        await using (var context = ExecuteAsOrganizationContextAsync(organizationA.Id))
        {
            await using var transaction =
                await context.Database.BeginTransactionAsync();

            await context.Organizations.AddAsync(organizationA);

            await context.SaveChangesAsync();
            await transaction.CommitAsync();
        }

        // Create Organization B and its property.
        await using (var context = ExecuteAsOrganizationContextAsync(organizationB.Id))
        {
            await using var transaction =
                await context.Database.BeginTransactionAsync();

            await context.Organizations.AddAsync(organizationB);

            var propertyB = Property.Register(
                organizationB.Id,
                PropertyName.Create(
                    $"Property B {Guid.NewGuid():N}"
                )
            );

            await context.Properties.AddAsync(propertyB);

            await context.SaveChangesAsync();

            propertyBId = propertyB.Id;

            await transaction.CommitAsync();
        }

        // Act 
        await using var organizationAContext = 
            ExecuteAsOrganizationContextAsync(organizationA.Id);

        await using var organizationATransaction = 
            await organizationAContext.Database.BeginTransactionAsync();

        var rowsAfected = 
            await organizationAContext.Database.ExecuteSqlInterpolatedAsync(
                $"""
                UPDATE "Properties"
                SET "Name" = {"Hacked Property"}
                WHERE "Id" = {propertyBId}
                """
            );

        Assert.Equal(0, rowsAfected);

        await organizationATransaction.RollbackAsync();
    }

    [Fact]
    public async Task Organization_should_not_delete_another_organizations_property()
    {
        // Arrange
        var organizationA =
            Organization.Register(
                OrganizationName.Create(
                    $"Organization A {Guid.NewGuid():N}"
                )
            );

        var organizationB =
            Organization.Register(
                OrganizationName.Create(
                    $"Organization B {Guid.NewGuid():N}"
                )
            );

        Guid propertyBId;

        // Create Organization A.
        await using (var context = ExecuteAsOrganizationContextAsync(organizationA.Id))
        {
            await using var transaction =
                await context.Database.BeginTransactionAsync();

            await context.Organizations.AddAsync(organizationA);

            await context.SaveChangesAsync();
            await transaction.CommitAsync();
        }

        // Create Organization B and its property.
        await using (var context = ExecuteAsOrganizationContextAsync(organizationB.Id))
        {
            await using var transaction =
                await context.Database.BeginTransactionAsync();

            await context.Organizations.AddAsync(organizationB);

            var propertyB =
                Property.Register(
                    organizationB.Id,
                    PropertyName.Create(
                        $"Property B {Guid.NewGuid():N}"
                    )
                );

            await context.Properties.AddAsync(propertyB);

            await context.SaveChangesAsync();

            propertyBId = propertyB.Id;

            await transaction.CommitAsync();
        }

        // Act
        await using var organizationAContext =
            ExecuteAsOrganizationContextAsync(organizationA.Id);

        await using var organizationATransaction =
            await organizationAContext.Database
                .BeginTransactionAsync();

        var rowsAffected =
            await organizationAContext.Database.ExecuteSqlInterpolatedAsync(
                $"""
                DELETE FROM "Properties"
                WHERE "Id" = {propertyBId}
                """
            );

        // Assert
        Assert.Equal(0, rowsAffected);

        await organizationATransaction.RollbackAsync();
    }
}