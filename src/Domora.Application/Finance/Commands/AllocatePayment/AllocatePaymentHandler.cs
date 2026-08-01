using Domora.Application.Common.Persistence;
using Domora.Domain.Common;
using Domora.Domain.Finance;

namespace Domora.Application.Finance.Commands.AllocatePayment;

public sealed class AllocatePaymentHandler
{
    private readonly IPaymentRepository _paymentRepository;

    private readonly IInvoiceRepository _invoiceRepository;

    private readonly IPaymentAllocationRepository _paymentAllocationRepository;

    private readonly IUnitOfWork _unitOfWork;

    public AllocatePaymentHandler(
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

    public async Task<AllocatePaymentResponse> Handle(
        AllocatePaymentCommand command,
        CancellationToken cancellationToken
    )
    {
        var payment = await _paymentRepository.GetByIdAsync(
            command.PaymentId,
            cancellationToken
        );

        if (payment is null)
            throw new InvalidOperationException("Payment not found.");

        var invoice = await _invoiceRepository.GetByIdAsync(
            command.InvoiceId,
            cancellationToken
        );

        if (invoice is null)
            throw new InvalidOperationException("Invoice not found.");  
        
        
        var allocatedToPayment = await _paymentAllocationRepository.GetAllocatedAmountForPaymentAsync(
            command.PaymentId,
            cancellationToken
        );
  
        var remaining = payment.Amount.Amount - allocatedToPayment.Amount;

        if (command.AllocateAmount.Amount > remaining)
            throw new InvalidOperationException(
                "Payment has insufficient remaining balance."
            );

        var allocatedToInvoice = await _paymentAllocationRepository.GetAllocatedAmountForInvoiceAsync(
            command.InvoiceId,
            cancellationToken
        );

        var outstanding = invoice.Amount.Amount - allocatedToInvoice.Amount;

        // if (command.AllocateAmount.Amount > outstanding)
        //     throw new InvalidOperationException(
        //         "Allocation amount exceeds invoice outstanding balance."
        //     );

        var allocationAmount = Math.Min( 
            command.AllocateAmount.Amount,
            Math.Min(remaining, outstanding)
        );

        var paymentAllocation = PaymentAllocation.Allocate(
            payment.Id,
            invoice.Id,
            new Money(allocationAmount, command.AllocateAmount.Currency)
        );

        await _paymentAllocationRepository.AddAsync(
            paymentAllocation,
            cancellationToken
        );

        var outstandingAfterAllocation = 
            outstanding - allocationAmount;

        if (outstandingAfterAllocation == 0)
        {
            invoice.MarkAsPaid();
        }


        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new AllocatePaymentResponse(
            paymentAllocation.Id,
            paymentAllocation.PaymentId,
            paymentAllocation.InvoiceId,
            paymentAllocation.AllocateAmount.Amount,
            paymentAllocation.AllocateAmount.Currency
        );
    } 
}