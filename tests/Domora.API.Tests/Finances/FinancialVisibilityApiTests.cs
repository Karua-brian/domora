using System.Net;
using Domora.API.Tests.Infrastructure;
using Domora.Application.Common.Context;
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
using Microsoft.Extensions.DependencyInjection;

namespace Domora.API.Tests.Finances;

[Collection("Domora API database")]
public sealed class FinancialVisibilityApiTests : IClassFixture<DomoraWebApplicationFactory>
{
    private readonly DomoraWebApplicationFactory _factory;

    public FinancialVisibilityApiTests(DomoraWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Financial_visibility_endpoints_authorize_roles_and_isolate_organizations()
    {
        Guid ownerAId, financeAId, memberAId, ownerBId, organizationAId, organizationBId;
        Guid invoiceAId, invoiceBId, tenantAId, tenantBId;

        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DomoraDbContext>();
            var organizationContext = scope.ServiceProvider.GetRequiredService<IOrganizationContext>();
            var ownerA = await TestData.CreateUserAsync(db);
            var financeA = await TestData.CreateUserAsync(db);
            var memberA = await TestData.CreateUserAsync(db);
            var ownerB = await TestData.CreateUserAsync(db);
            var organizationA = await TestData.CreateOrganizationAsync(db, ownerA.Id);
            var organizationB = await TestData.CreateOrganizationAsync(db, ownerB.Id);
            await TestData.AddMembershipAsync(db, financeA.Id, organizationA.Id, OrganizationRole.Finance);
            await TestData.AddMembershipAsync(db, memberA.Id, organizationA.Id, OrganizationRole.Member);

            tenantAId = Guid.NewGuid();
            tenantBId = Guid.NewGuid();
            var (propertyA, unitA, leaseA, invoiceA) = CreateInvoice(organizationA.Id, tenantAId, "A");
            var (propertyB, unitB, leaseB, invoiceB) = CreateInvoice(organizationB.Id, tenantBId, "B");

            await SaveOrganizationGraphAsync(organizationA.Id, propertyA, unitA, leaseA, invoiceA);
            await SaveOrganizationGraphAsync(organizationB.Id, propertyB, unitB, leaseB, invoiceB);

            ownerAId = ownerA.Id;
            financeAId = financeA.Id;
            memberAId = memberA.Id;
            ownerBId = ownerB.Id;
            organizationAId = organizationA.Id;
            organizationBId = organizationB.Id;
            invoiceAId = invoiceA.Id;
            invoiceBId = invoiceB.Id;

            async Task SaveOrganizationGraphAsync(
                Guid organizationId,
                Property property,
                Unit unit,
                Lease lease,
                Invoice invoice)
            {
                organizationContext.Set(organizationId);
                await using var transaction = await db.Database.BeginTransactionAsync();
                await db.Properties.AddAsync(property);
                await db.Units.AddAsync(unit);
                await db.Leases.AddAsync(lease);
                await db.Invoices.AddAsync(invoice);
                await db.SaveChangesAsync();
                await transaction.CommitAsync();
            }
        }

        using var ownerAClient = CreateClient(ownerAId, organizationAId);
        using var financeAClient = CreateClient(financeAId, organizationAId);
        using var memberAClient = CreateClient(memberAId, organizationAId);
        using var ownerBClient = CreateClient(ownerBId, organizationBId);

        foreach (var path in VisibilityPaths)
        {
            Assert.Equal(HttpStatusCode.OK, (await ownerAClient.GetAsync(path)).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await financeAClient.GetAsync(path)).StatusCode);
            var denied = await memberAClient.GetAsync(path);
            Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
            Assert.DoesNotContain(invoiceAId.ToString(), await denied.Content.ReadAsStringAsync());
        }

        var invoicesA = await ownerAClient.GetStringAsync(VisibilityPaths[0]);
        Assert.Contains(invoiceAId.ToString(), invoicesA);
        Assert.DoesNotContain(invoiceBId.ToString(), invoicesA);

        var balancesA = await ownerAClient.GetStringAsync(VisibilityPaths[1]);
        Assert.Contains(tenantAId.ToString(), balancesA);
        Assert.DoesNotContain(tenantBId.ToString(), balancesA);

        var arrearsA = await ownerAClient.GetStringAsync(VisibilityPaths[2]);
        Assert.Contains(invoiceAId.ToString(), arrearsA);
        Assert.DoesNotContain(invoiceBId.ToString(), arrearsA);

        var invoicesB = await ownerBClient.GetStringAsync(VisibilityPaths[0]);
        Assert.Contains(invoiceBId.ToString(), invoicesB);
        Assert.DoesNotContain(invoiceAId.ToString(), invoicesB);
    }

    private static readonly string[] VisibilityPaths =
    [
        "/api/finances/invoices",
        "/api/finances/tenant-balances",
        "/api/finances/arrears"
    ];

    private HttpClient CreateClient(Guid userId, Guid organizationId)
    {
        var client = AuthenticatedClientFactory.Create(_factory, userId);
        client.DefaultRequestHeaders.Add("X-Organization-Id", organizationId.ToString());
        return client;
    }

    private static (Property Property, Unit Unit, Lease Lease, Invoice Invoice) CreateInvoice(
        Guid organizationId, Guid tenantId, string key)
    {
        var property = Property.Register(organizationId, PropertyName.Create($"Visibility {key} {Guid.NewGuid():N}"));
        var unit = Unit.Register(property.Id, UnitNumber.Create($"{key}-{Guid.NewGuid():N}"), UnitType.Apartment);
        var lease = Lease.Register(unit.Id, tenantId, new Money(100m, "KES"));
        var invoice = Invoice.Create(organizationId, lease.Id, InvoiceType.Rent,
            new Money(100m, "KES"), new DateOnly(2000, 1, 1));
        return (property, unit, lease, invoice);
    }
}
