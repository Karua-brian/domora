
using Domora.Application.Common.Context;
using Domora.Application.Common.Authorization;
using Domora.Application.Common.Exceptions;
using Domora.Application.Finance.Commands.AllocatePayment;
using Domora.Domain.Common;
using Domora.Domain.Common.Exceptions;
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
using Domora.Infrastructure.Persistence.Interceptors;
using Domora.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Domora.Application.Tests.Finance.Commands.AllocatePayment;

public sealed class AllocatePaymentHandlerTests
{
    private sealed class AllowFinancialAuthorization : IFinancialAuthorization
    {
        public Task EnsureCanAsync(
            FinancialCapability capability,
            CancellationToken cancellationToken = default
        ) => Task.CompletedTask;
    }

    private readonly string _connectionString =
        Environment.GetEnvironmentVariable("DomoraTest")
        ?? throw new InvalidOperationException(
            "DomoraTest connection string is not configured."
        );

    private sealed class TestOrganizationContext : IOrganizationContext
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

        var options =
            new DbContextOptionsBuilder<DomoraDbContext>()
                .UseNpgsql(_connectionString)
                .AddInterceptors(interceptor)
                .Options;

        return new DomoraDbContext(options);
    }

    private async Task<(
        Guid OrganizationId,
        Guid LeaseId,
        Guid InvoiceId,
        Guid PaymentId
    )> SeedPaymentScenarioAsync()
    {
        var organization = Organization.Register(
            OrganizationName.Create(
                $"Allocation Test Org {Guid.NewGuid():N}"
            )
        );

        var property = Property.Register(
            organization.Id,
            PropertyName.Create(
                $"Allocation Property {Guid.NewGuid():N}"
            )
        );

        var unit = Unit.Register(
            property.Id,
            UnitNumber.Create(
                $"ALLOC-{Guid.NewGuid():N}"
            ),
            UnitType.Bedsitter
        );

        var lease = Lease.Register(
            unit.Id,
            Guid.NewGuid(),
            new Money(10_000m, "KES")
        );

        var invoice = Invoice.Create(
            organization.Id,
            lease.Id,
            InvoiceType.Rent,
            new Money(10_000m, "KES"),
            DateOnly.FromDateTime(
                DateTime.UtcNow.AddDays(7)
            )
        );

        var payment = Payment.Receive(
            organization.Id,
            new Money(5_000m, "KES"),
            $"ALLOC-{Guid.NewGuid():N}"
        );

        await using var context =
            CreateContext(organization.Id);

        await using var transaction =
            await context.Database.BeginTransactionAsync();

        await context.Set<Organization>()
            .AddAsync(organization);

        await context.Set<Property>()
            .AddAsync(property);

        await context.Set<Unit>()
            .AddAsync(unit);

        await context.Set<Lease>()
            .AddAsync(lease);

        await context.Set<Invoice>()
            .AddAsync(invoice);

        await context.Set<Payment>()
            .AddAsync(payment);

        await context.SaveChangesAsync();
        await transaction.CommitAsync();

        return (
            organization.Id,
            lease.Id,
            invoice.Id,
            payment.Id
        );
    }

    [Fact]
    public async Task Allocate_partial_payment_should_update_invoice_and_payment_balance()
    {
        // Arrange
        var seeded = await SeedPaymentScenarioAsync();

        await using var context =
            CreateContext(seeded.OrganizationId);

        var organizationContext =
            new TestOrganizationContext(
                seeded.OrganizationId
            );

        var paymentRepository =
            new PaymentRepository(context);

        var leaseRepository = 
            new LeaseRepository(context);

        var invoiceRepository =
            new InvoiceRepository(context);

        var paymentAllocationRepository =
            new PaymentAllocationRepository(context);

        var unitOfWork =
            new UnitOfWork(
                context,
                organizationContext
            );

        var handler =
            new AllocatePaymentHandler(
                paymentRepository,
                leaseRepository,
                invoiceRepository,
                paymentAllocationRepository,
                unitOfWork,
                new AllowFinancialAuthorization()
            );

        var command =
            new AllocatePaymentCommand(
                seeded.PaymentId,
                seeded.LeaseId,
                new Money(4_000m, "KES")
            );

        // Act
        var response = await handler.Handle(
            command,
            CancellationToken.None
        );

        // Assert response
        Assert.Equal(
            seeded.PaymentId,
            response.PaymentId
        );

        Assert.Equal(
            4_000m,
            response.TotalAllocated
        );

        Assert.Equal(
            "KES",
            response.Currency
        );

        var responseAllocation =
            Assert.Single(response.Allocations);

        Assert.Equal(
            seeded.InvoiceId,
            responseAllocation.InvoiceId
        );

        Assert.Equal(
            4_000m,
            responseAllocation.Amount
        );

        // Assert persisted state using a fresh context.
        await using var verificationContext =
            CreateContext(seeded.OrganizationId);

        await using var verificationTransaction =
            await verificationContext.Database
                .BeginTransactionAsync();

        var persistedInvoice =
            await verificationContext.Set<Invoice>()
                .SingleAsync(
                    x => x.Id == seeded.InvoiceId
                );

        var persistedPayment =
            await verificationContext.Set<Payment>()
                .SingleAsync(
                    x => x.Id == seeded.PaymentId
                );

        var persistedAllocations =
            await verificationContext
                .Set<PaymentAllocation>()
                .Where(
                    x => x.PaymentId == seeded.PaymentId
                )
                .ToListAsync();

        Assert.Equal(
            InvoiceStatus.PartiallyPaid,
            persistedInvoice.Status
        );

        var outstandingBalance =
            persistedInvoice.GetOutstandingBalance(
                new Money(4_000m, "KES")
            );

        Assert.Equal(
            6_000m,
            outstandingBalance.Amount
        );

        Assert.Equal(
            1_000m,
            persistedPayment.UnallocatedAmount.Amount
        );

        var persistedAllocation =
            Assert.Single(persistedAllocations);

        Assert.Equal(
            seeded.OrganizationId,
            persistedAllocation.OrganizationId
        );

        Assert.Equal(
            seeded.InvoiceId,
            persistedAllocation.InvoiceId
        );

        Assert.Equal(
            4_000m,
            persistedAllocation.AllocateAmount.Amount
        );
    }

    [Fact]
    public async Task 
        Allocating_exceeding_available_balance_should_throw_domain_validation_exception_and_preserve_state()
    {
        // Arrange
        var seeded = await SeedPaymentScenarioAsync();

        await using var context = CreateContext(seeded.OrganizationId);
        var organizationContext = 
            new TestOrganizationContext(seeded.OrganizationId);

        var leaseRepository = 
            new LeaseRepository(context);

        var paymentRepository = 
            new PaymentRepository(context);

        var invoiceRepository = 
            new InvoiceRepository(context);

        var paymentAllocationRepository = 
            new PaymentAllocationRepository(context);

        var unitOfWork = 
            new UnitOfWork(context, organizationContext);

        var handler = new AllocatePaymentHandler(
            paymentRepository,
            leaseRepository,
            invoiceRepository,
            paymentAllocationRepository,
            unitOfWork,
            new AllowFinancialAuthorization()
        );

        var firstCommand = new AllocatePaymentCommand(
            seeded.PaymentId,
            seeded.LeaseId,
            new Money(3_000m, "KES")
        );

        await handler.Handle(
            firstCommand,
            CancellationToken.None
        );

        var secondCommand = new AllocatePaymentCommand(
            seeded.PaymentId,
            seeded.LeaseId,
            new Money(3_000m, "KES")
        );

        // Act & Assert
        await Assert.ThrowsAsync<DomainValidationException>(
            async () =>
            {
                await handler.Handle(secondCommand, CancellationToken.None);
            });

        // Assert persisted state: Verify database remains uncorrupted by the second request
        await using var verificationContext =
            CreateContext(seeded.OrganizationId);

        await using var transaction =
            await verificationContext.Database.
                BeginTransactionAsync(CancellationToken.None);

        var persistedInvoice = 
            await verificationContext.Set<Invoice>()
                .SingleAsync(
                    x => x.Id == seeded.InvoiceId
                );

        var persistedPayment = 
            await verificationContext.Set<Payment>()
                .SingleAsync(
                    x => x.Id == seeded.PaymentId
                );

        var persistedAllocations = 
            await verificationContext.Set<PaymentAllocation>()
            .Where(
                x => x.PaymentId == seeded.PaymentId
            )
            .ToListAsync();

        Assert.Equal(
            2_000m, 
            persistedPayment.UnallocatedAmount.Amount
        );

        Assert.Equal(
            InvoiceStatus.PartiallyPaid, 
            persistedInvoice.Status
        );
        
        var outstandingBalance = 
            persistedInvoice.GetOutstandingBalance(
                new Money(3_000m, "KES")
            );

        Assert.Equal(
            7_000m, 
            outstandingBalance.Amount
        );

        var singlePersistedAllocation = 
            Assert.Single(persistedAllocations);

        Assert.Equal(
            3_000m, 
            singlePersistedAllocation.AllocateAmount.Amount
        );

        Assert.Equal(
            5_000m,
            persistedPayment.TotalAmount.Amount
        );

        Assert.Equal(
            seeded.InvoiceId, 
            singlePersistedAllocation.InvoiceId
        );

        Assert.Equal(
            seeded.OrganizationId, 
            singlePersistedAllocation.OrganizationId
        );
    }


    [Fact]
    public async Task
        Allocating_payment_to_another_organizations_lease_should_be_rejected()
    {
        // Arrange: two independent rental businesses.
        var organizationA = await SeedPaymentScenarioAsync();
        var organizationB = await SeedPaymentScenarioAsync();

        await using var context = CreateContext(
            organizationA.OrganizationId
        );

        var organizationContext = new TestOrganizationContext(
            organizationA.OrganizationId
        );

        var paymentRepository = 
            new PaymentRepository(context);

        var leaseRepository = 
            new LeaseRepository(context);

        var invoiceRepository = 
            new InvoiceRepository(context);

        var paymentAllocationRepository =
            new PaymentAllocationRepository(context);
            
        var unitOfWork = new UnitOfWork(
            context,
            organizationContext
        );

        var handler = new AllocatePaymentHandler(
            paymentRepository,
            leaseRepository,
            invoiceRepository,
            paymentAllocationRepository,
            unitOfWork,
            new AllowFinancialAuthorization()
        );

        var command = new AllocatePaymentCommand(
            organizationA.PaymentId,
            organizationB.LeaseId,
            new Money(1_000m, "KES")
        );

        // Act & Assert:
        // Organization A's payment cannot settle Organization B's lease.
        await Assert.ThrowsAsync<NotFoundException>(
            async () => await handler.Handle(
                command,
                CancellationToken.None
            )
        );
    }

}
