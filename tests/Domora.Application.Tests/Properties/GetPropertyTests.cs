using Domora.Application.Common.Context;
using Domora.Application.Common.Exceptions;
using Domora.Application.Properties.Queries.GetProperty;
using Domora.Domain.Organizations;
using Domora.Domain.Organizations.ValueObjects;
using Domora.Domain.Properties;
using Domora.Domain.Properties.ValueObjects;
using Domora.Infrastructure.Persistence;
using Domora.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Domora.Application.Tests.Properties;

public sealed class GetPropertyTests
{
    private readonly DbContextOptions<DomoraDbContext> _options;

    public GetPropertyTests()
    {
        var connectionString = Environment.GetEnvironmentVariable("DomoraTest");

        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException(
                "DomoraTest connection string is not configured."
            );

        _options = new DbContextOptionsBuilder<DomoraDbContext>()
            .UseNpgsql(connectionString)
            .Options;
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
        await using var context = new DomoraDbContext(_options);

        var organization = Organization.Register(
            OrganizationName.Create($"Same Org Test {Guid.NewGuid():N}")
        );

        await context.Organizations.AddAsync(organization);

        var property = Property.Register(
            organization.Id,
            PropertyName.Create($"Same Org Property {Guid.NewGuid():N}")
        );

        await context.Properties.AddAsync(property);

        await context.SaveChangesAsync();

        var organizationContext = new TestOrganizationContext(organization.Id);

        var handler = new GetPropertyHandler(
            new PropertyRepository(context),
            organizationContext
        );

        // Act
        var response = await handler.Handle(
            new GetPropertyQuery(property.Id),
            CancellationToken.None
        );

        // Assert
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

        await context.Organizations.AddRangeAsync(
            organizationA,
            organizationB
        );

        var property = Property.Register(
            organizationA.Id,
            PropertyName.Create($"Property A {Guid.NewGuid():N}")
        );

        await context.Properties.AddAsync(property);

        await context.SaveChangesAsync();

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

        await context.Organizations.AddAsync(organization);
        await context.SaveChangesAsync();

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