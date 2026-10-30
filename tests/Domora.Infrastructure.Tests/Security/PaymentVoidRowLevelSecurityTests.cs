using Domora.Application.Common.Context;
using Domora.Domain.Common;
using Domora.Domain.Finance;
using Domora.Domain.Users;
using Domora.Domain.Users.ValueObjects;
using Domora.Infrastructure.Persistence;
using Domora.Infrastructure.Persistence.Interceptors;
using Microsoft.EntityFrameworkCore;

namespace Domora.Infrastructure.Tests.Security;

public sealed class PaymentVoidRowLevelSecurityTests
{
    private readonly string _connectionString =
        Environment.GetEnvironmentVariable("DomoraTest")
        ?? throw new InvalidOperationException("DomoraTest connection string is not configured.");

    private sealed class TestOrganizationContext(Guid organizationId) : IOrganizationContext
    {
        public Guid OrganizationId { get; private set; } = organizationId;
        public void Set(Guid value) => OrganizationId = value;
    }

    private DomoraDbContext CreateContext(Guid organizationId)
    {
        var interceptor = new OrganizationTransactionInterceptor(new TestOrganizationContext(organizationId));
        var options = new DbContextOptionsBuilder<DomoraDbContext>()
            .UseNpgsql(_connectionString).AddInterceptors(interceptor).Options;
        return new DomoraDbContext(options);
    }

    [Fact]
    public async Task Another_organization_cannot_read_or_update_payment_void()
    {
        var seeded = await SeedAsync();
        await using var context = CreateContext(Guid.NewGuid());
        await using var transaction = await context.Database.BeginTransactionAsync();

        Assert.Null(await context.PaymentVoids.SingleOrDefaultAsync(x => x.Id == seeded.VoidId));
        var updatedRows = await context.PaymentVoids.Where(x => x.Id == seeded.VoidId)
            .ExecuteUpdateAsync(update => update.SetProperty(x => x.Reason, "tampered"));
        Assert.Equal(0, updatedRows);
    }

    [Fact]
    public async Task Another_organization_cannot_insert_payment_void_for_foreign_payment()
    {
        var seeded = await SeedAsync();
        await using var context = CreateContext(Guid.NewGuid());
        await using var transaction = await context.Database.BeginTransactionAsync();

        await context.PaymentVoids.AddAsync(PaymentVoid.Create(
            seeded.OrganizationId, seeded.PaymentId, seeded.ActorUserId, "unauthorized"));
        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [Fact]
    public async Task Composite_foreign_key_rejects_payment_from_different_organization()
    {
        var seeded = await SeedAsync();
        var otherOrganizationId = Guid.NewGuid();
        var otherPayment = Payment.Receive(
            otherOrganizationId, new Money(50m, "KES"), $"OTHER-{Guid.NewGuid():N}");

        await using (var otherContext = CreateContext(otherOrganizationId))
        await using (var otherTransaction = await otherContext.Database.BeginTransactionAsync())
        {
            await otherContext.Payments.AddAsync(otherPayment);
            await otherContext.SaveChangesAsync();
            await otherTransaction.CommitAsync();
        }

        await using var context = CreateContext(seeded.OrganizationId);
        await using var transaction = await context.Database.BeginTransactionAsync();
        await context.PaymentVoids.AddAsync(PaymentVoid.Create(
            seeded.OrganizationId, otherPayment.Id, seeded.ActorUserId, "cross-organization link"));

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    private async Task<(Guid OrganizationId, Guid PaymentId, Guid ActorUserId, Guid VoidId)> SeedAsync()
    {
        var organizationId = Guid.NewGuid();
        var user = User.Register(
            UserEmail.Create($"void-{Guid.NewGuid():N}@example.com"),
            UserPassword.Create("Hashed-password"));
        var payment = Payment.Receive(organizationId, new Money(100m, "KES"), $"VOID-{Guid.NewGuid():N}");
        payment.Void();
        var paymentVoid = PaymentVoid.Create(organizationId, payment.Id, user.Id, "seed void");

        await using var context = CreateContext(organizationId);
        await using var transaction = await context.Database.BeginTransactionAsync();
        await context.Users.AddAsync(user);
        await context.Payments.AddAsync(payment);
        await context.PaymentVoids.AddAsync(paymentVoid);
        await context.SaveChangesAsync();
        await transaction.CommitAsync();

        return (organizationId, payment.Id, user.Id, paymentVoid.Id);
    }
}
