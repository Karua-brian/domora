using Domora.Application.Common.Exceptions;
using Domora.Application.Common.Persistence;
using Domora.Domain.Common;
using Domora.Domain.Common.Exceptions;
using Domora.Domain.Finance;
using Domora.Domain.Leasing;

namespace Domora.Application.Finance.Commands.AllocatePayment;

public sealed class AllocatePaymentHandler
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly ILeaseRepository _leaseRepository;
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly IPaymentAllocationRepository _paymentAllocationRepository;
    private readonly IUnitOfWork _unitOfWork;

    public AllocatePaymentHandler(
        IPaymentRepository paymentRepository,
        ILeaseRepository leaseRepository,
        IInvoiceRepository invoiceRepository,
        IPaymentAllocationRepository paymentAllocationRepository,
        IUnitOfWork unitOfWork
    )
    {
        _paymentRepository = paymentRepository;
        _leaseRepository = leaseRepository;
        _invoiceRepository = invoiceRepository;
        _paymentAllocationRepository = paymentAllocationRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<AllocatePaymentResponse> Handle(
        AllocatePaymentCommand command,
        CancellationToken cancellationToken
    )
    {
        var allocationsCreated = new List<PaymentAllocationResponse>();

        await using var transaction = 
            await _unitOfWork.BeginTransactionAsync(cancellationToken);

        var payment = await _paymentRepository.GetByIdAsync(
            command.PaymentId,
            cancellationToken
        );
        if (payment is null)
            throw new NotFoundException("Payment not found."); 
        
        var allocatedToPaymentSoFar = await _paymentAllocationRepository
            .GetAllocatedAmountForPaymentAsync(
                command.PaymentId,
                payment.TotalAmount.Currency,
                cancellationToken
            );

        payment.EnsureCanAllocate(
            command.AllocateAmount,
            allocatedToPaymentSoFar
        );

        var lease = await _leaseRepository.GetByIdAsync(
            command.LeaseId,
            cancellationToken
        );

        if (lease is null)
            throw new NotFoundException("Lease not found.");

        // if (lease.OrganizationId != payment.OrganizationId)
        //     throw new NotFoundException("Lease not found.");


        var unpaidInvoices = await _invoiceRepository.GetUnpaidInvoicesByLeaseIdAsync(
            command.LeaseId,
            cancellationToken
        ); 

        var remainingCashToAllocate = command.AllocateAmount.Amount;
        decimal totalAllocatedInThisSession = 0;

        // WATERFALL EXECUTION LOOP
        foreach (var invoice in unpaidInvoices)
        {
            if (invoice.OrganizationId != payment.OrganizationId)
                    throw new DomainValidationException(
                        "Payment and invoice must belong to the same organization."
                );

            if (remainingCashToAllocate <= 0)
                break;

            var allocatedToInvoiceSoFar = await _paymentAllocationRepository
                .GetAllocatedAmountForInvoiceAsync(
                    invoice.Id,
                    invoice.Amount.Currency,
                    cancellationToken
                );

            var outstanding = invoice.GetOutstandingBalance(allocatedToInvoiceSoFar);
            if (outstanding.Amount <= 0)
                continue;

            var availableCashPool = new Money(
                remainingCashToAllocate,
                command.AllocateAmount.Currency
            );

            var actualConsumedAmount = invoice.AllocatePayment(
                availableCashPool,
                allocatedToInvoiceSoFar
            );

            if (actualConsumedAmount > 0)
            {
                var consumptionMoney = new Money(
                    actualConsumedAmount,
                    command.AllocateAmount.Currency
                );

                payment.DeductCredit(consumptionMoney);

                var paymentAllocation = PaymentAllocation.Allocate(
                    payment.OrganizationId,
                    payment.Id,
                    invoice.Id,
                    new Money(
                        actualConsumedAmount,
                        command.AllocateAmount.Currency
                    )
                );

                await _paymentAllocationRepository.AddAsync(
                    paymentAllocation,
                    cancellationToken
                );

                remainingCashToAllocate -= actualConsumedAmount;
                totalAllocatedInThisSession += actualConsumedAmount;

                allocationsCreated.Add(
                    new PaymentAllocationResponse(
                        paymentAllocation.Id,
                        invoice.Id,
                        actualConsumedAmount,
                        command.AllocateAmount.Currency
                    )
                );
            }   
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        return new AllocatePaymentResponse(
            payment.Id,
            totalAllocatedInThisSession,
            command.AllocateAmount.Currency,
            allocationsCreated
        );
    } 
}