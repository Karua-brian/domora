using Domora.Application.Common.Context;
using Domora.Application.Properties.Commands.RegisterProperty;
using Domora.Domain.Organizations;
using Domora.Domain.Organizations.ValueObjects;
using Domora.Infrastructure.Persistence;
using Domora.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Domora.Application.Tests.Properties;

public sealed class RegisterPropertyTests
{
    private readonly DbContextOptions<DomoraDbContext> _options;

    public RegisterPropertyTests()
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
    public async Task Register_property_should_use_organization_from_context()
    {
        // Arrange
        var organizationId = Guid.NewGuid();

        await using var context = new DomoraDbContext(_options);

        var organization = Organization.Register(
            OrganizationName.Create(
                $"Property Test Org {Guid.NewGuid():N}"
            )
        );

        await context.Organizations.AddAsync(organization);
        await context.SaveChangesAsync();

        // Use the actual persisted organization Id
        organizationId = organization.Id;

        var organizationContext = new TestOrganizationContext(organizationId);

        var handler = new RegisterPropertyHandler(
            new PropertyRepository(context),
            organizationContext,
            new UnitOfWork(context)
        );

        var command = new RegisterPropertyCommand(
            $"Property {Guid.NewGuid():N}"
        );

        // Act
        var response = await handler.Handle(
            command,
            CancellationToken.None
        );

        // Assert
        Assert.Equal(
            organizationId,
            response.OrganizationId
        );

        await using var verificationContext = new DomoraDbContext(_options);

        var persistedProperty = await verificationContext.Properties
            .SingleAsync(p => p.Id == response.Id);

        Assert.Equal(
            organizationId,
            persistedProperty.OrganizationId
        );
    }
}