using Domora.Application.Common.Context;
using Domora.Application.Common.Exceptions;
using Domora.Application.Properties.Queries.GetProperty;
using Domora.Domain.Organizations;
using Domora.Domain.Organizations.ValueObjects;
using Domora.Domain.Properties;
using Domora.Domain.Properties.ValueObjects;
using Domora.Infrastructure.Persistence;
using Domora.Infrastructure.Persistence.Interceptors;
using Domora.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Domora.Application.Tests.Properties;

public sealed class GetPropertyTests
{
    private readonly DbContextOptions<DomoraDbContext> _options;

    private readonly string _connectionString;

    public GetPropertyTests()
    {
        _connectionString = 
        Environment.GetEnvironmentVariable("DomoraTest")
        ??  throw new InvalidOperationException(
                "DomoraTest connection string is not configured."
        );

        _options = new DbContextOptionsBuilder<DomoraDbContext>()
            .UseNpgsql(_connectionString)
            .Options;
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

    private sealed class TestOrganizationContext : IOrganizationContext
    {
        public Guid OrganizationId { get; }

        public TestOrganizationContext(Guid organizationId)
        {
            OrganizationId = organizationId;
        }
    }

    [Fact]
    public async Task Same_organization_should_return_property()
    {
        // Arrange
        var organization = Organization.Register(
            OrganizationName.Create($"Same Org Test {Guid.NewGuid():N}")
        );

        var property = Property.Register(
            organization.Id,
            PropertyName.Create($"Same Org Property {Guid.NewGuid():N}")
        );

        await ExecuteAsOrganizationContextAsync(
            organization.Id,
            async ctx =>
            {
                await ctx.Organizations.AddAsync(organization);
                await ctx.Properties.AddAsync(property);
                await ctx.SaveChangesAsync();
            }
        );

        // Act
        GetPropertyResponse? response = null;

        await ExecuteAsOrganizationContextAsync(
            organization.Id,
            async ctx =>
            {
                var handler = new GetPropertyHandler(
                    new PropertyRepository(ctx),
                    new TestOrganizationContext(organization.Id)
                );

                response = await handler.Handle(
                    new GetPropertyQuery(property.Id),
                    CancellationToken.None
                );
            }
        );

        // Assert
        Assert.NotNull(response);
        Assert.Equal(property.Id, response.Id);
        Assert.Equal(organization.Id, response.OrganizationId);
    }

    [Fact]
    public async Task Cross_organization_should_return_not_found()
    {
        // Arrange
        await using var context = new DomoraDbContext(_options);

        var organizationA = Organization.Register(
            OrganizationName.Create($"Organization A {Guid.NewGuid():N}")
        );

        var organizationB = Organization.Register(
            OrganizationName.Create($"Organization B {Guid.NewGuid():N}")
        );

        var property = Property.Register(
            organizationA.Id,
            PropertyName.Create($"Property A {Guid.NewGuid():N}")
        );

        await ExecuteAsOrganizationContextAsync(
            organizationA.Id,
            async ctx =>
            {
                await ctx.Organizations.AddAsync(organizationA);
                await ctx.Organizations.AddAsync(organizationB);
                await ctx.Properties.AddAsync(property);
                await ctx.SaveChangesAsync();
            }
        );

        // IMPORTANT:
        // The current user belongs to Organization B
        var organizationContext = new TestOrganizationContext(organizationB.Id);

        var handler = new GetPropertyHandler(
            new PropertyRepository(context),
            organizationContext
        );
        
        // Act
        var exception = await Assert.ThrowsAsync<NotFoundException>(
            () => handler.Handle(
                new GetPropertyQuery(property.Id),
                CancellationToken.None
            )
        );

        // Assert
        Assert.Equal("Property not found.", exception.Message);
    }

    [Fact]
    public async Task Nonexistent_property_should_return_404()
    {
        // Arrange
        await using var context = new DomoraDbContext(_options);

        var organization = Organization.Register(
            OrganizationName.Create($"Nonexistent Test Org {Guid.NewGuid():N}")
        );

        await ExecuteAsOrganizationContextAsync(
            organization.Id,
            async ctx =>
            {
                await ctx.Organizations.AddAsync(organization);
                await ctx.SaveChangesAsync();
            }
        );

        var organizationContext = new TestOrganizationContext(organization.Id);

        var handler = new GetPropertyHandler(
            new PropertyRepository(context),
            organizationContext
        );

        var nonexistentPropertyId = Guid.NewGuid();

        // Act
        var exception = await Assert.ThrowsAsync<NotFoundException>(
            () => handler.Handle(
                new GetPropertyQuery(nonexistentPropertyId),
                CancellationToken.None
            )
        );

        // Assert
        Assert.Equal(
            "Property not found.",
            exception.Message
        );
    }
}