using System.Data.Common;
using System.Net;
using System.Net.Http.Json;
using Domora.API.Tests.Infrastructure;
using Domora.Application.Common.Authorization;
using Domora.Application.Common.Context;
using Domora.Application.Common.Exceptions;
using Domora.Domain.Common;
using Domora.Domain.Finance;
using Domora.Domain.Finance.Enums;
using Domora.Domain.Leasing;
using Domora.Domain.Organizations.Enums;
using Domora.Domain.Properties;
using Domora.Domain.Properties.ValueObjects;
using Domora.Domain.Units;
using Domora.Domain.Units.Enums;
using Domora.Domain.Units.ValueObjects;
using Domora.Infrastructure.Persistence;
using Domora.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace Domora.API.Tests.Organizations;

[Collection("Domora API database")]
public sealed class MembershipRemovalTests : IClassFixture<DomoraWebApplicationFactory>
{
    private sealed class TestAuthorizationContext(Guid userId, Guid organizationId)
        : IUserContext, IOrganizationContext
    {
        public Guid UserId { get; } = userId;
        public Guid OrganizationId { get; } = organizationId;
        public void Set(Guid value) => throw new NotSupportedException();
    }

    private readonly DomoraWebApplicationFactory _factory;

    public MembershipRemovalTests(DomoraWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Owner_can_remove_member_and_finance_and_removed_users_lose_access()
    {
        var seeded = await SeedAsync();
        using var owner = CreateClient(seeded.OwnerId, seeded.OrganizationId);
        using var member = CreateClient(seeded.MemberId, seeded.OrganizationId);
        using var finance = CreateClient(seeded.FinanceId, seeded.OrganizationId);

        Assert.Equal(HttpStatusCode.NoContent,
            (await RemoveAsync(owner, seeded.MemberId)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent,
            (await RemoveAsync(owner, seeded.FinanceId)).StatusCode);

        Assert.Equal(HttpStatusCode.Forbidden, (await ReceivePaymentAsync(member)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await ReceivePaymentAsync(finance)).StatusCode);
        await AssertMembershipAbsentAsync(seeded.OrganizationId, seeded.MemberId);
        await AssertMembershipAbsentAsync(seeded.OrganizationId, seeded.FinanceId);

        await AssertAuthorizationRevokedAsync(seeded.OrganizationId, seeded.MemberId);
        await AssertAuthorizationRevokedAsync(seeded.OrganizationId, seeded.FinanceId);
    }

    [Fact]
    public async Task Nonowner_cannot_remove_another_member()
    {
        var seeded = await SeedAsync();
        using var finance = CreateClient(seeded.FinanceId, seeded.OrganizationId);

        Assert.Equal(HttpStatusCode.Forbidden,
            (await RemoveAsync(finance, seeded.MemberId)).StatusCode);
        await AssertRoleAsync(seeded.OrganizationId, seeded.MemberId, OrganizationRole.Member);
    }

    [Fact]
    public async Task Member_and_finance_can_leave_and_then_lose_organization_access()
    {
        var seeded = await SeedAsync();
        using var member = CreateClient(seeded.MemberId, seeded.OrganizationId);
        using var finance = CreateClient(seeded.FinanceId, seeded.OrganizationId);

        Assert.Equal(HttpStatusCode.NoContent, (await LeaveAsync(member)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await LeaveAsync(finance)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await ReceivePaymentAsync(member)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await ReceivePaymentAsync(finance)).StatusCode);
        await AssertMembershipAbsentAsync(seeded.OrganizationId, seeded.MemberId);
        await AssertMembershipAbsentAsync(seeded.OrganizationId, seeded.FinanceId);

        await AssertAuthorizationRevokedAsync(seeded.OrganizationId, seeded.MemberId);
        await AssertAuthorizationRevokedAsync(seeded.OrganizationId, seeded.FinanceId);
    }

    [Fact]
    public async Task Owner_cannot_remove_themselves_or_leave()
    {
        var seeded = await SeedAsync();
        using var owner = CreateClient(seeded.OwnerId, seeded.OrganizationId);

        Assert.Equal(HttpStatusCode.Conflict,
            (await RemoveAsync(owner, seeded.OwnerId)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await LeaveAsync(owner)).StatusCode);
        await AssertRoleAsync(seeded.OrganizationId, seeded.OwnerId, OrganizationRole.Owner);
    }

    [Fact]
    public async Task Membership_removal_preserves_business_and_audit_records()
    {
        var seeded = await SeedWithHistoricalRecordsAsync();
        using var owner = CreateClient(seeded.OwnerId, seeded.OrganizationId);

        Assert.Equal(HttpStatusCode.NoContent,
            (await RemoveAsync(owner, seeded.MemberId)).StatusCode);

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<DomoraDbContext>();
        scope.ServiceProvider.GetRequiredService<IOrganizationContext>().Set(seeded.OrganizationId);
        await using var transaction = await db.Database.BeginTransactionAsync();
        await using (DbCommand command = db.Database.GetDbConnection().CreateCommand())
        {
            command.Transaction = db.Database.CurrentTransaction!.GetDbTransaction();
            command.CommandText = "SELECT set_config('app.organization_id', @organization_id, true)";
            var parameter = command.CreateParameter();
            parameter.ParameterName = "organization_id";
            parameter.Value = seeded.OrganizationId.ToString();
            command.Parameters.Add(parameter);
            await command.ExecuteScalarAsync();
        }
        Assert.True(await db.Users.AnyAsync(x => x.Id == seeded.MemberId));
        Assert.True(await db.Properties.AnyAsync(x => x.Id == seeded.PropertyId));
        Assert.True(await db.Units.AnyAsync(x => x.Id == seeded.UnitId));
        Assert.True(await db.Leases.AnyAsync(x => x.Id == seeded.LeaseId));
        Assert.True(await db.Invoices.AnyAsync(x => x.Id == seeded.InvoiceId));
        Assert.True(await db.Payments.AnyAsync(x => x.Id == seeded.PaymentId));
        Assert.True(await db.PaymentAllocations.AnyAsync(x => x.Id == seeded.AllocationId));
        var paymentVoid = await db.PaymentVoids.SingleAsync(x => x.PaymentId == seeded.PaymentId);
        Assert.Equal(seeded.MemberId, paymentVoid.ActorUserId);
        await transaction.CommitAsync();
    }

    [Fact]
    public async Task Concurrent_removal_and_ownership_transfer_leave_exactly_one_owner()
    {
        var seeded = await SeedAsync();
        using var ownerA = CreateClient(seeded.OwnerId, seeded.OrganizationId);
        using var ownerB = CreateClient(seeded.OwnerId, seeded.OrganizationId);

        var responses = await Task.WhenAll(
            RemoveAsync(ownerA, seeded.MemberId),
            TransferAsync(ownerB, seeded.MemberId));

        var successful = responses.Count(x =>
            x.StatusCode is HttpStatusCode.NoContent or HttpStatusCode.OK);
        Assert.Equal(1, successful);
        Assert.Contains(responses.Select(x => x.StatusCode), status =>
            status is HttpStatusCode.NotFound or HttpStatusCode.Forbidden);

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<DomoraDbContext>();
        var memberships = await db.OrganizationMemberships
            .Where(x => x.OrganizationId == seeded.OrganizationId)
            .ToListAsync();
        Assert.Single(memberships, x => x.Role == OrganizationRole.Owner);
    }

    private async Task<SeededMemberships> SeedAsync()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<DomoraDbContext>();
        var owner = await TestData.CreateUserAsync(db);
        var finance = await TestData.CreateUserAsync(db);
        var member = await TestData.CreateUserAsync(db);
        var organization = await TestData.CreateOrganizationAsync(db, owner.Id);
        await TestData.AddMembershipAsync(db, finance.Id, organization.Id, OrganizationRole.Finance);
        await TestData.AddMembershipAsync(db, member.Id, organization.Id, OrganizationRole.Member);
        return new SeededMemberships(organization.Id, owner.Id, finance.Id, member.Id);
    }

    private async Task<HistoricalSeed> SeedWithHistoricalRecordsAsync()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<DomoraDbContext>();
        var owner = await TestData.CreateUserAsync(db);
        var member = await TestData.CreateUserAsync(db);
        var organization = await TestData.CreateOrganizationAsync(db, owner.Id);
        await TestData.AddMembershipAsync(db, member.Id, organization.Id, OrganizationRole.Finance);

        var property = Property.Register(
            organization.Id,
            PropertyName.Create($"Removal history {Guid.NewGuid():N}"));
        var unit = Unit.Register(
            property.Id,
            UnitNumber.Create($"RM-{Guid.NewGuid():N}"),
            UnitType.Apartment);
        var lease = Lease.Register(unit.Id, member.Id, new Money(1000m, "KES"));
        var invoice = Invoice.Create(
            organization.Id,
            lease.Id,
            InvoiceType.Rent,
            new Money(1000m, "KES"),
            DateOnly.FromDateTime(DateTime.UtcNow));
        var payment = Payment.Receive(
            organization.Id,
            new Money(500m, "KES"),
            $"REMOVE-{Guid.NewGuid():N}");
        payment.Void();
        var allocation = PaymentAllocation.Allocate(
            organization.Id,
            payment.Id,
            invoice.Id,
            new Money(250m, "KES"));
        var paymentVoid = PaymentVoid.Create(
            organization.Id,
            payment.Id,
            member.Id,
            "Historical actor reference");

        await using (var transaction = await db.Database.BeginTransactionAsync())
        {
            await using DbCommand command = db.Database.GetDbConnection().CreateCommand();
            command.Transaction = db.Database.CurrentTransaction!.GetDbTransaction();
            command.CommandText = "SELECT set_config('app.organization_id', @organization_id, true)";
            var parameter = command.CreateParameter();
            parameter.ParameterName = "organization_id";
            parameter.Value = organization.Id.ToString();
            command.Parameters.Add(parameter);
            await command.ExecuteScalarAsync();

            await db.Properties.AddAsync(property);
            await db.Units.AddAsync(unit);
            await db.Leases.AddAsync(lease);
            await db.Invoices.AddAsync(invoice);
            await db.Payments.AddAsync(payment);
            await db.PaymentAllocations.AddAsync(allocation);
            await db.PaymentVoids.AddAsync(paymentVoid);
            await db.SaveChangesAsync();
            await transaction.CommitAsync();
        }

        return new HistoricalSeed(
            organization.Id, owner.Id, member.Id, property.Id, unit.Id,
            lease.Id, invoice.Id, payment.Id, allocation.Id);
    }

    private HttpClient CreateClient(Guid userId, Guid organizationId)
    {
        var client = AuthenticatedClientFactory.Create(_factory, userId);
        client.DefaultRequestHeaders.Add("X-Organization-Id", organizationId.ToString());
        return client;
    }

    private static Task<HttpResponseMessage> RemoveAsync(HttpClient client, Guid userId) =>
        client.DeleteAsync($"/api/organizations/memberships/{userId}");

    private static Task<HttpResponseMessage> LeaveAsync(HttpClient client) =>
        client.PostAsync("/api/organizations/memberships/leave", content: null);

    private static Task<HttpResponseMessage> TransferAsync(HttpClient client, Guid newOwnerUserId) =>
        client.PostAsJsonAsync(
            "/api/organizations/ownership/transfer",
            new { newOwnerUserId });

    private static Task<HttpResponseMessage> ReceivePaymentAsync(HttpClient client) =>
        client.PostAsJsonAsync("/api/finances/payments", new
        {
            amount = 10m,
            currency = "KES",
            reference = $"REMOVED-{Guid.NewGuid():N}"
        });

    private async Task AssertRoleAsync(Guid organizationId, Guid userId, OrganizationRole role)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<DomoraDbContext>();
        var membership = await db.OrganizationMemberships.SingleAsync(x =>
            x.OrganizationId == organizationId && x.UserId == userId);
        Assert.Equal(role, membership.Role);
    }

    private async Task AssertMembershipAbsentAsync(Guid organizationId, Guid userId)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<DomoraDbContext>();
        Assert.False(await db.OrganizationMemberships.AnyAsync(x =>
            x.OrganizationId == organizationId && x.UserId == userId));
    }

    private async Task AssertAuthorizationRevokedAsync(Guid organizationId, Guid userId)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<DomoraDbContext>();
        var memberships = new OrganizationMembershipRepository(db);
        var organizationAccess = new OrganizationAccess(memberships);
        var context = new TestAuthorizationContext(userId, organizationId);
        var financialAuthorization = new FinancialAuthorization(context, context, memberships);

        Assert.False(await organizationAccess.CanAccessAsync(
            userId,
            organizationId,
            CancellationToken.None));
        await Assert.ThrowsAsync<ForbiddenException>(() => financialAuthorization.EnsureCanAsync(
            FinancialCapability.ReceivePayments));
    }

    private sealed record SeededMemberships(
        Guid OrganizationId,
        Guid OwnerId,
        Guid FinanceId,
        Guid MemberId);

    private sealed record HistoricalSeed(
        Guid OrganizationId,
        Guid OwnerId,
        Guid MemberId,
        Guid PropertyId,
        Guid UnitId,
        Guid LeaseId,
        Guid InvoiceId,
        Guid PaymentId,
        Guid AllocationId);
}
