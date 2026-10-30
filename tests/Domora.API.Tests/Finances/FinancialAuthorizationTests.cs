using System.Net;
using System.Net.Http.Json;
using Domora.API.Tests.Infrastructure;
using Domora.Domain.Common;
using Domora.Domain.Finance;
using Domora.Domain.Finance.Enums;
using Domora.Domain.Leasing;
using Domora.Domain.Organizations;
using Domora.Domain.Organizations.Enums;
using Domora.Domain.Organizations.ValueObjects;
using Domora.Domain.Properties;
using Domora.Domain.Properties.ValueObjects;
using Domora.Domain.Units;
using Domora.Domain.Units.Enums;
using Domora.Domain.Units.ValueObjects;
using Domora.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Domora.API.Tests.Finances;

public sealed class FinancialAuthorizationTests : IClassFixture<DomoraWebApplicationFactory>
{
    private readonly DomoraWebApplicationFactory _factory;

    public FinancialAuthorizationTests(DomoraWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Financial_endpoints_enforce_the_application_role_matrix()
    {
        Guid organizationId;
        Guid ownerId;
        Guid financeId;
        Guid memberId;
        Guid nonmemberId;
        Guid leaseId;
        Guid paymentId;
        string ownerPaymentReference;

        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DomoraDbContext>();
            var owner = await TestData.CreateUserAsync(db);
            var finance = await TestData.CreateUserAsync(db);
            var member = await TestData.CreateUserAsync(db);
            var nonmember = await TestData.CreateUserAsync(db);
            var organization = await TestData.CreateOrganizationAsync(db);
            await TestData.AddMembershipAsync(db, owner.Id, organization.Id, OrganizationRole.Owner);
            await TestData.AddMembershipAsync(db, finance.Id, organization.Id, OrganizationRole.Finance);
            await TestData.AddMembershipAsync(db, member.Id, organization.Id, OrganizationRole.Member);

            var property = Property.Register(
                organization.Id,
                PropertyName.Create($"Financial authorization {Guid.NewGuid():N}"));
            var unit = Unit.Register(
                property.Id,
                UnitNumber.Create($"FA-{Guid.NewGuid():N}"),
                UnitType.Apartment);
            var lease = Lease.Register(unit.Id, Guid.NewGuid(), new Money(2_000m, "KES"));
            var invoice = Invoice.Create(
                organization.Id,
                lease.Id,
                InvoiceType.Rent,
                new Money(2_000m, "KES"),
                DateOnly.FromDateTime(DateTime.UtcNow));
            var payment = Payment.Receive(
                organization.Id,
                new Money(1_000m, "KES"),
                $"AUTH-ALLOC-{Guid.NewGuid():N}");
            var seededOwnerPayment = Payment.Receive(
                organization.Id,
                new Money(100m, "KES"),
                $"AUTH-VOID-{Guid.NewGuid():N}");

            scope.ServiceProvider.GetRequiredService<Domora.Application.Common.Context.IOrganizationContext>()
                .Set(organization.Id);
            await using (var transaction = await db.Database.BeginTransactionAsync())
            {
                await db.Properties.AddAsync(property);
                await db.Units.AddAsync(unit);
                await db.Leases.AddAsync(lease);
                await db.Invoices.AddAsync(invoice);
                await db.Payments.AddRangeAsync(payment, seededOwnerPayment);
                await db.SaveChangesAsync();
                await transaction.CommitAsync();
            }

            organizationId = organization.Id;
            ownerId = owner.Id;
            financeId = finance.Id;
            memberId = member.Id;
            nonmemberId = nonmember.Id;
            leaseId = lease.Id;
            paymentId = payment.Id;
            ownerPaymentReference = seededOwnerPayment.Reference;
        }

        using var ownerClient = CreateClient(ownerId, organizationId);
        using var financeClient = CreateClient(financeId, organizationId);
        using var memberClient = CreateClient(memberId, organizationId);
        using var nonmemberClient = CreateClient(nonmemberId, organizationId);

        Assert.Equal(HttpStatusCode.Created, (await Receive(ownerClient, "OWNER")).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await Receive(financeClient, "FINANCE")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await Receive(memberClient, "MEMBER")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await Receive(nonmemberClient, "NONMEMBER")).StatusCode);

        Assert.Equal(HttpStatusCode.Created, (await Allocate(ownerClient, paymentId, leaseId)).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await Allocate(financeClient, paymentId, leaseId)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await Allocate(memberClient, paymentId, leaseId)).StatusCode);

        Assert.Equal(HttpStatusCode.Created, (await IssueInvoice(ownerClient, leaseId, InvoiceType.Garbage)).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await IssueInvoice(financeClient, leaseId, InvoiceType.Rent)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await IssueInvoice(memberClient, leaseId, InvoiceType.Garbage)).StatusCode);

        using var paymentReadScope = _factory.Services.CreateScope();
        var verifyDb = paymentReadScope.ServiceProvider.GetRequiredService<DomoraDbContext>();
        paymentReadScope.ServiceProvider.GetRequiredService<Domora.Application.Common.Context.IOrganizationContext>()
            .Set(organizationId);
        await using var readTransaction = await verifyDb.Database.BeginTransactionAsync();
        var persistedOwnerPayment = await verifyDb.Payments.SingleAsync(x => x.Reference == ownerPaymentReference);
        var ownerPaymentId = persistedOwnerPayment.Id;
        await readTransaction.CommitAsync();

        var financeVoid = await financeClient.PostAsJsonAsync(
            $"/api/finances/payments/{ownerPaymentId}/void",
            new { reason = "Finance role cannot void" });
        Assert.Equal(HttpStatusCode.Forbidden, financeVoid.StatusCode);

        var memberVoid = await memberClient.PostAsJsonAsync(
            $"/api/finances/payments/{ownerPaymentId}/void",
            new { reason = "Member role cannot void" });
        Assert.Equal(HttpStatusCode.Forbidden, memberVoid.StatusCode);

        var ownerVoid = await ownerClient.PostAsJsonAsync(
            $"/api/finances/payments/{ownerPaymentId}/void",
            new { reason = "Owner authorized void" });
        Assert.Equal(HttpStatusCode.OK, ownerVoid.StatusCode);
    }

    private HttpClient CreateClient(Guid userId, Guid organizationId)
    {
        var client = AuthenticatedClientFactory.Create(_factory, userId);
        client.DefaultRequestHeaders.Add("X-Organization-Id", organizationId.ToString());
        return client;
    }

    private static Task<HttpResponseMessage> Receive(HttpClient client, string prefix) =>
        client.PostAsJsonAsync("/api/finances/payments", new
        {
            amount = 50m,
            currency = "KES",
            reference = $"{prefix}-{Guid.NewGuid():N}"
        });

    private static Task<HttpResponseMessage> Allocate(HttpClient client, Guid paymentId, Guid leaseId) =>
        client.PostAsJsonAsync("/api/finances/payment-allocations", new
        {
            paymentId,
            leaseId,
            amount = 100m,
            currency = "KES"
        });

    private static Task<HttpResponseMessage> IssueInvoice(
        HttpClient client,
        Guid leaseId,
        InvoiceType type) =>
        client.PostAsJsonAsync("/api/finances/invoices", new
        {
            leaseId,
            type = type.ToString(),
            amount = 100m,
            currency = "KES",
            dueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(15))
        });
}
