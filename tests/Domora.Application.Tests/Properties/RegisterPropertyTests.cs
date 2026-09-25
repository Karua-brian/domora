using Domora.Application.Common.Context;
using Domora.Application.Common.Exceptions;
using Domora.Application.Properties.Commands.RegisterProperty;
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

public sealed class RegisterPropertyTests
{
    private readonly DbContextOptions<DomoraDbContext> _options;

    private readonly string _connectionString;

    public RegisterPropertyTests()
    {
        _connectionString = Environment.GetEnvironmentVariable("DomoraTest")
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
    private async Task<T> ExecuteAsOrganizationContextAsync<T>(
        Guid organizationId,
        Func<DomoraDbContext, Task<T>> action)
    {
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

        await using var transaction =
            await context.Database.BeginTransactionAsync();

        var result = await action(context);

        await transaction.CommitAsync();

        return result;
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
        var organization = Organization.Register(
            OrganizationName.Create(
                $"Property Test Org {Guid.NewGuid():N}"
            )
        );

        await ExecuteAsOrganizationContextAsync(
            organization.Id,
            async (ctx) =>
            {
                await ctx.Organizations.AddAsync(organization);
                await ctx.SaveChangesAsync();
            }
        );

        var command = new RegisterPropertyCommand(
            $"Property {Guid.NewGuid():N}"
        );

        // Act
        var response = await ExecuteAsOrganizationContextAsync(
            organization.Id,
            async (ctx) =>
            {
                var propertyRepository =
                    new PropertyRepository(ctx);

                var unitOfWork = new UnitOfWork(
                    ctx,
                    new TestOrganizationContext(organization.Id)
                );

                var handler = new RegisterPropertyHandler(
                    propertyRepository,
                    new TestOrganizationContext(organization.Id),
                    unitOfWork
                );

                return await handler.Handle(
                    command,
                    CancellationToken.None
                );
            }
        );

        // Assert
        Assert.Equal(
            organization.Id,
            response.OrganizationId
        );

        // Verify through the same organization boundary
        var persistedProperty =
            await ExecuteAsOrganizationContextAsync(
                organization.Id,
                async ctx =>
                {
                    return await ctx.Properties
                        .SingleAsync(p => p.Id == response.Id);
                }
        );
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