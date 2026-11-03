using System.Net;
using System.Net.Http.Json;
using Domora.API.Tests.Infrastructure;
using Domora.Application.Common.Context;
using Domora.Domain.Common;
using Domora.Domain.Finance;
using Domora.Domain.Finance.Enums;
using Domora.Domain.Leasing;
using Domora.Domain.Properties;
using Domora.Domain.Properties.ValueObjects;
using Domora.Domain.Units;
using Domora.Domain.Units.Enums;
using Domora.Domain.Units.ValueObjects;
using Domora.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Domora.API.Tests.Finances;

[Collection("Domora API database")]
public sealed class PaymentAllocationOwnershipTests : IClassFixture<DomoraWebApplicationFactory>
{
    private readonly DomoraWebApplicationFactory _factory;

    public PaymentAllocationOwnershipTests(DomoraWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Payment_cannot_be_allocated_to_another_organizations_lease()
    {
        Guid userAId, organizationAId, leaseBId;
        Payment paymentA;
        Invoice invoiceA;

        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DomoraDbContext>();
            var organizationContext = scope.ServiceProvider.GetRequiredService<IOrganizationContext>();
            var userA = await TestData.CreateUserAsync(db);
            var userB = await TestData.CreateUserAsync(db);
            var organizationA = await TestData.CreateOrganizationAsync(db, userA.Id);
            var organizationB = await TestData.CreateOrganizationAsync(db, userB.Id);

            var propertyA = Property.Register(organizationA.Id, PropertyName.Create($"Allocation A {Guid.NewGuid():N}"));
            var unitA = Unit.Register(propertyA.Id, UnitNumber.Create($"A-{Guid.NewGuid():N}"), UnitType.Apartment);
            var leaseA = Lease.Register(unitA.Id, Guid.NewGuid(), new Money(100m, "KES"));
            invoiceA = Invoice.Create(organizationA.Id, leaseA.Id, InvoiceType.Rent,
                new Money(100m, "KES"), new DateOnly(2026, 1, 1));
            paymentA = Payment.Receive(organizationA.Id, new Money(100m, "KES"), $"OWNERSHIP-{Guid.NewGuid():N}");

            var propertyB = Property.Register(organizationB.Id, PropertyName.Create($"Allocation B {Guid.NewGuid():N}"));
            var unitB = Unit.Register(propertyB.Id, UnitNumber.Create($"B-{Guid.NewGuid():N}"), UnitType.Apartment);
            var leaseB = Lease.Register(unitB.Id, Guid.NewGuid(), new Money(100m, "KES"));

            organizationContext.Set(organizationA.Id);
            await using (var transaction = await db.Database.BeginTransactionAsync())
            {
                await db.Properties.AddAsync(propertyA);
                await db.Units.AddAsync(unitA);
                await db.Leases.AddAsync(leaseA);
                await db.Invoices.AddAsync(invoiceA);
                await db.Payments.AddAsync(paymentA);
                await db.SaveChangesAsync();
                await transaction.CommitAsync();
            }

            organizationContext.Set(organizationB.Id);
            await using (var transaction = await db.Database.BeginTransactionAsync())
            {
                await db.Properties.AddAsync(propertyB);
                await db.Units.AddAsync(unitB);
                await db.Leases.AddAsync(leaseB);
                await db.SaveChangesAsync();
                await transaction.CommitAsync();
            }

            userAId = userA.Id;
            organizationAId = organizationA.Id;
            leaseBId = leaseB.Id;
        }

        using var client = AuthenticatedClientFactory.Create(_factory, userAId);
        client.DefaultRequestHeaders.Add("X-Organization-Id", organizationAId.ToString());
        var response = await client.PostAsJsonAsync("/api/finances/payment-allocations", new
        {
            paymentId = paymentA.Id,
            leaseId = leaseBId,
            amount = 50m,
            currency = "KES"
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        await using var verifyScope = _factory.Services.CreateAsyncScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<DomoraDbContext>();
        verifyScope.ServiceProvider.GetRequiredService<IOrganizationContext>().Set(organizationAId);
        await using var verifyTransaction = await verifyDb.Database.BeginTransactionAsync();
        var persistedPayment = await verifyDb.Payments.SingleAsync(x => x.Id == paymentA.Id);
        var persistedInvoice = await verifyDb.Invoices.SingleAsync(x => x.Id == invoiceA.Id);
        Assert.Equal(100m, persistedPayment.UnallocatedAmount.Amount);
        Assert.Equal(InvoiceStatus.Pending, persistedInvoice.Status);
        Assert.Empty(await verifyDb.PaymentAllocations.Where(x => x.PaymentId == paymentA.Id).ToListAsync());
        await verifyTransaction.CommitAsync();
    }
}
