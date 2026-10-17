using Domora.Application.Common.Context;
using Domora.Domain.Common;
using Domora.Domain.Finance;
using Domora.Infrastructure.Persistence;
using Domora.Infrastructure.Persistence.Interceptors;
using Microsoft.EntityFrameworkCore;

namespace Domora.Infrastructure.Tests.Security;

public sealed class PaymentRowLevelSecurityTests
{
    private readonly string _connectionString =
        Environment.GetEnvironmentVariable("DomoraTest")
        ?? throw new InvalidOperationException(
            "DomoraTest connection string is not configured."
        );

    public sealed class TestOrganizationContext : IOrganizationContext
    {
        public Guid OrganizationId { get; private set; }
        public TestOrganizationContext(Guid organizationId)
        {
            OrganizationId = organizationId;
        }

        public void Set(Guid organizationId)
        {
            OrganizationId = organizationId;
        }
    }

    private DomoraDbContext CreateContext(Guid organizationId)
    {
        var organizationContext =
            new TestOrganizationContext(organizationId);

        var interceptor =
            new OrganizationTransactionInterceptor(
                organizationContext
            );

        var options = new DbContextOptionsBuilder<DomoraDbContext>()
            .UseNpgsql(_connectionString)
            .AddInterceptors(interceptor)
            .Options;

        return new DomoraDbContext(options);
    }

    [Fact]
    public async Task Organization_should_read_its_own_payment()
    {
        // Arrange
        var organizationId = Guid.NewGuid();
        var payment = Payment.Receive(
            organizationId,
            new Money(100, "KES"),
            $"payment-{Guid.NewGuid():N}"
        );

        await SeedPaymentAsync(
            organizationId,
            payment.Id,
            payment
        );

        // Act
        await using var context = CreateContext(organizationId);

        await using var transaction =
            await context.Database.BeginTransactionAsync();

        var result = await context.Set<Payment>()
            .SingleOrDefaultAsync(p => p.Id == payment.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(
            organizationId, 
            result.OrganizationId
        );
    }

    [Fact]
    public async Task Organization_should_not_read_another_organizations_payment()
    {
        // Arrange
        var ownerOrganizationId = Guid.NewGuid();
        var otherOrganizationId = Guid.NewGuid();

        var payment = Payment.Receive(
            ownerOrganizationId,
            new Money(100, "KES"),
            $"payment-{Guid.NewGuid():N}"
        );

        await SeedPaymentAsync(
            ownerOrganizationId,
            payment.Id,
            payment
        );

        // Act
        await using var context = CreateContext(otherOrganizationId);

        await using var transaction =
            await context.Database.BeginTransactionAsync();

        var result = await context.Set<Payment>()
            .SingleOrDefaultAsync(p => p.Id == payment.Id);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task Organization_should_not_insert_payment_for_another_organization()
    {
        // Arrange
        var ownerOrganizationId = Guid.NewGuid();
        var actingOrganizationId = Guid.NewGuid();

        var payment = Payment.Receive(
            ownerOrganizationId,
            new Money(100, "KES"),
            $"payment-{Guid.NewGuid():N}"
        );

        await SeedPaymentAsync(
            ownerOrganizationId,
            payment.Id,
            payment
        );

        // Act
        await using var context = CreateContext(actingOrganizationId);

        await using var transaction =
            await context.Database.BeginTransactionAsync();

        await context.Set<Payment>().AddAsync(payment);

        // Assert
        await Assert.ThrowsAsync<DbUpdateException>(
            () => context.SaveChangesAsync()
        );
    }

    [Fact]
    public async Task Organization_should_not_update_another_organizations_payment()
    {
        // Arrange
        var ownerOrganizationId = Guid.NewGuid();
        var otherOrganizationId = Guid.NewGuid();

        var payment = Payment.Receive(
            ownerOrganizationId,
            new Money(100, "KES"),
            $"payment-{Guid.NewGuid():N}"
        );

        await SeedPaymentAsync(
            ownerOrganizationId,
            payment.Id,
            payment
        );

        // Act
        await using var context = CreateContext(otherOrganizationId);

        await using var transaction =
            await context.Database.BeginTransactionAsync();

        var affectedRows = await context.Set<Payment>()
            .Where(p => p.Id == payment.Id)
            .ExecuteUpdateAsync(setters => 
                setters.SetProperty(
                    p => p.Reference,
                    "unauthorized-reference"
                ));

        // Assert
        Assert.Equal(
            0,
            affectedRows
        );
    }

    [Fact]
    public async Task Organization_should_not_delete_another_organizations_payment()
    {
        // Arrange
        var ownerOrganizationId = Guid.NewGuid();
        var otherOrganizationId = Guid.NewGuid();

        var payment = Payment.Receive(
            ownerOrganizationId,
            new Money(100, "KES"),
            $"payment-{Guid.NewGuid():N}"
        );

        await SeedPaymentAsync(
            ownerOrganizationId,
            payment.Id,
            payment
        );

        // Act
        await using var context = CreateContext(otherOrganizationId);

        await using var transaction =
            await context.Database.BeginTransactionAsync();

        var affectedRows = await context.Set<Payment>()
            .Where(p => p.Id == payment.Id)
            .ExecuteDeleteAsync();

        // Assert
        Assert.Equal(
            0,
            affectedRows
        );
    }
    
    private async Task SeedPaymentAsync(
        Guid organizationId,
        Guid paymentId,
        Payment payment
    )
    {
        await using var context = CreateContext(organizationId);
        await using var transaction =
            await context.Database.BeginTransactionAsync();

        await context.Set<Payment>().AddAsync(payment);
        await context.SaveChangesAsync();
        await transaction.CommitAsync();
    }
}