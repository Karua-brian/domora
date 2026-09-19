using Domora.Application.Common.Context;
using Domora.Application.Common.Exceptions;
using Domora.Application.Properties.Commands.RegisterProperty;
using Domora.Application.Properties.Queries.GetProperty;
using Domora.Domain.Organizations;
using Domora.Domain.Organizations.ValueObjects;
using Domora.Domain.Properties;
using Domora.Domain.Properties.ValueObjects;
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
        await using var context = new DomoraDbContext(_options);

        var organization = Organization.Register(
            OrganizationName.Create(
                $"Property Test Org {Guid.NewGuid():N}"
            )
        );

        await context.Organizations.AddAsync(organization);
        await context.SaveChangesAsync();

        // Use the actual persisted organization Id
        var organizationContext = new TestOrganizationContext(organization.Id);

        var handler = new RegisterPropertyHandler(
            new PropertyRepository(context),
            organizationContext,
            new UnitOfWork(context, organizationContext)
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
            organization.Id,
            response.OrganizationId
        );

        await using var verificationContext = new DomoraDbContext(_options);

        var persistedProperty = await verificationContext.Properties
            .SingleAsync(p => p.Id == response.Id);

        Assert.Equal(
            organization.Id,
            persistedProperty.OrganizationId
        );
    }

    [Fact]
    public async Task Beginning_transaction_without_organization_context_should_fail()
    {
        await using var context = new DomoraDbContext(_options);

        var organizationContext = new TestOrganizationContext(Guid.Empty);

        var unitOfWork = new UnitOfWork(
            context,
            organizationContext
        );

        await Assert.ThrowsAsync<InvalidOperationException>(
            async () =>
            {
                await unitOfWork.BeginTransactionAsync(CancellationToken.None);
            }
        );

    }
}