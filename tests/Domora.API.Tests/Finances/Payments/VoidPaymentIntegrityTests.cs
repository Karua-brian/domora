using System.Net;
using System.Data.Common;
using System.Net.Http.Json;
using Domora.API.Tests.Infrastructure;
using Domora.Application.Common.Context;
using Domora.Domain.Common;
using Domora.Domain.Finance;
using Domora.Domain.Finance.Enums;
using Domora.Domain.Leasing;
using Domora.Domain.Organizations;
using Domora.Domain.Organizations.ValueObjects;
using Domora.Domain.Properties;
using Domora.Domain.Properties.ValueObjects;
using Domora.Domain.Units;
using Domora.Domain.Units.Enums;
using Domora.Domain.Units.ValueObjects;
using Domora.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace Domora.API.Tests.Finances.Payments;

public sealed class VoidPaymentIntegrityTests : IClassFixture<DomoraWebApplicationFactory>
{
    private readonly DomoraWebApplicationFactory _factory;

    public VoidPaymentIntegrityTests(DomoraWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Void_preserves_allocations_and_excludes_only_voided_payment_from_invoice_balance()
    {
        Guid userId;
        Guid organizationId;
        Guid paymentId;
        Guid invoiceId;
        Guid leaseId;
        Guid firstAllocationId;
        Guid secondPaymentId;
        Guid secondAllocationId;
        string reference;

        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DomoraDbContext>();
            var user = await TestData.CreateUserAsync(db);
            var organization = await TestData.CreateOrganizationAsync(db);
            await TestData.AddMembershipAsync(db, user.Id, organization.Id);

            var property = Property.Register(organization.Id, PropertyName.Create($"Void {Guid.NewGuid():N}"));
            var unit = Unit.Register(property.Id, UnitNumber.Create($"V-{Guid.NewGuid():N}"), UnitType.Apartment);
            var lease = Lease.Register(unit.Id, Guid.NewGuid(), new Money(1000m, "KES"));
            var invoice = Invoice.Create(organization.Id, lease.Id, InvoiceType.Rent, new Money(1000m, "KES"), DateOnly.FromDateTime(DateTime.UtcNow));
            reference = $"VOID-{Guid.NewGuid():N}";
            var payment = Payment.Receive(organization.Id, new Money(500m, "KES"), reference);
            var otherPayment = Payment.Receive(organization.Id, new Money(400m, "KES"), $"LIVE-{Guid.NewGuid():N}");
            var allocation = PaymentAllocation.Allocate(organization.Id, payment.Id, invoice.Id, new Money(300m, "KES"));
            var otherAllocation = PaymentAllocation.Allocate(organization.Id, otherPayment.Id, invoice.Id, new Money(400m, "KES"));
            invoice.RecalculatePaymentStatus(new Money(700m, "KES"));

            await using (var transaction = await db.Database.BeginTransactionAsync())
            {
                await using var command = db.Database.GetDbConnection().CreateCommand();
                command.Transaction = db.Database.CurrentTransaction!.GetDbTransaction();
                command.CommandText = "SELECT set_config('app.organization_id', @organization_id, true)";
                var organizationParameter = command.CreateParameter();
                organizationParameter.ParameterName = "organization_id";
                organizationParameter.Value = organization.Id.ToString();
                command.Parameters.Add(organizationParameter);
                await command.ExecuteScalarAsync();

                await db.Properties.AddAsync(property);
                await db.Units.AddAsync(unit);
                await db.Leases.AddAsync(lease);
                await db.Invoices.AddAsync(invoice);
                await db.Payments.AddRangeAsync(payment, otherPayment);
                await db.PaymentAllocations.AddRangeAsync(allocation, otherAllocation);
                await db.SaveChangesAsync();
                await transaction.CommitAsync();
            }

            userId = user.Id;
            organizationId = organization.Id;
            paymentId = payment.Id;
            invoiceId = invoice.Id;
            leaseId = lease.Id;
            firstAllocationId = allocation.Id;
            secondPaymentId = otherPayment.Id;
            secondAllocationId = otherAllocation.Id;
        }

        using var client = AuthenticatedClientFactory.Create(_factory, userId);
        client.DefaultRequestHeaders.Add("X-Organization-Id", organizationId.ToString());
        var voidTask = client.PostAsJsonAsync(
            $"/api/finances/payments/{paymentId}/void",
            new { reason = "Recorded against the wrong tenant" });
        var allocationTask = client.PostAsJsonAsync(
            "/api/finances/payment-allocations",
            new { paymentId, leaseId, amount = 100m, currency = "KES" });
        var raceResponses = await Task.WhenAll(voidTask, allocationTask);
        Assert.Contains(raceResponses[0].StatusCode, new[] { HttpStatusCode.OK, HttpStatusCode.Conflict });
        Assert.Contains(raceResponses[1].StatusCode, new[] { HttpStatusCode.Created, HttpStatusCode.Conflict });

        if (raceResponses[0].StatusCode == HttpStatusCode.Conflict)
        {
            var retryVoid = await client.PostAsJsonAsync(
                $"/api/finances/payments/{paymentId}/void",
                new { reason = "Recorded against the wrong tenant" });
            Assert.Equal(HttpStatusCode.OK, retryVoid.StatusCode);
        }

        await using var verifyScope = _factory.Services.CreateAsyncScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<DomoraDbContext>();
        verifyScope.ServiceProvider.GetRequiredService<IOrganizationContext>().Set(organizationId);
        await using var verifyTransaction = await verifyDb.Database.BeginTransactionAsync();
        var persistedPayment = await verifyDb.Payments.SingleAsync(x => x.Id == paymentId);
        var paymentVoid = await verifyDb.PaymentVoids.SingleAsync(x => x.PaymentId == paymentId);
        var invoiceAfter = await verifyDb.Invoices.SingleAsync(x => x.Id == invoiceId);
        var allocations = await verifyDb.PaymentAllocations.Where(x => x.InvoiceId == invoiceId).ToListAsync();

        Assert.Equal(PaymentStatus.Voided, persistedPayment.Status);
        Assert.Equal("Recorded against the wrong tenant", paymentVoid.Reason);
        Assert.Equal(userId, paymentVoid.ActorUserId);
        Assert.Equal(organizationId, paymentVoid.OrganizationId);
        Assert.Equal(TimeSpan.Zero, paymentVoid.VoidedAtUtc.Offset);
        Assert.InRange(allocations.Count, 2, 3);
        Assert.Contains(allocations, x => x.Id == firstAllocationId && x.AllocateAmount.Amount == 300m);
        Assert.Contains(allocations, x => x.Id == secondAllocationId && x.AllocateAmount.Amount == 400m);

        var effectiveAllocated = await (
            from allocation in verifyDb.PaymentAllocations
            join paymentRow in verifyDb.Payments on allocation.PaymentId equals paymentRow.Id
            where allocation.InvoiceId == invoiceId && paymentRow.Status == PaymentStatus.Active
            select allocation.AllocateAmount.Amount).SumAsync();
        Assert.Equal(400m, effectiveAllocated);
        Assert.Equal(600m, invoiceAfter.GetOutstandingBalance(new Money(effectiveAllocated, "KES")).Amount);
        Assert.Equal(InvoiceStatus.PartiallyPaid, invoiceAfter.Status);
        Assert.Equal(PaymentStatus.Active, (await verifyDb.Payments.SingleAsync(x => x.Id == secondPaymentId)).Status);

        var retryResponse = await client.PostAsJsonAsync(
            "/api/finances/payments",
            new { amount = 300m, currency = "KES", reference });
        Assert.Equal(HttpStatusCode.Conflict, retryResponse.StatusCode);

        var secondVoidResponse = await client.PostAsJsonAsync(
            $"/api/finances/payments/{paymentId}/void",
            new { reason = "Duplicate void attempt" });
        Assert.Equal(HttpStatusCode.Conflict, secondVoidResponse.StatusCode);
    }
}
