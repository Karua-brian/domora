using Domora.Application.Common.Exceptions;
using Domora.Application.Common.Persistence;
using Domora.Domain.Finance;

namespace Domora.Application.Finance.Commands.VoidPayment;

public sealed class VoidPaymentHandler
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly IPaymentAllocationRepository _paymentAllocationRepository;
    private readonly IUnitOfWork _unitOfWork;

    public VoidPaymentHandler(
        IPaymentRepository paymentRepository,
        IInvoiceRepository invoiceRepository,
        IPaymentAllocationRepository paymentAllocationRepository,
        IUnitOfWork unitOfWork
    )
    {
        _paymentRepository = paymentRepository;
        _invoiceRepository = invoiceRepository;
        _paymentAllocationRepository = paymentAllocationRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<VoidPaymentResponse> Handle(
        VoidPaymentCommand command,
        CancellationToken cancellationToken
    )
    {
        var payment = await _paymentRepository.GetByIdAsync(
            command.PaymentId,
            cancellationToken
        );
        if (payment is null)
            throw new NotFoundException(
                "Payment entry not found."
            );

        var linkedAllocations = await _paymentAllocationRepository
            .GetAllocationsByPaymentIdAsync(
                payment.Id,
                cancellationToken
            );
        
        foreach (var allocation in linkedAllocations)
        {
            var invoice = await _invoiceRepository.GetByIdAsync(
                allocation.InvoiceId,
                cancellationToken
            );
            if (invoice is null) continue;

            var historicalAllocationTotal = await _paymentAllocationRepository
                .GetAllocatedAmountForInvoiceAsync(
                    invoice.Id,
                    invoice.Amount.Currency,
                    cancellationToken
                );

            invoice.ReverseAllocation(
                allocation.AllocateAmount,
                historicalAllocationTotal
            );

            _paymentAllocationRepository.Remove(allocation);
        }

        payment.Void();

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new VoidPaymentResponse(
            payment.Id,
            payment.Status.ToString()
        );
    }
}