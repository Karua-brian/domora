using Domora.Application.Common.Exceptions;
using Domora.Application.Common.Persistence;
using Domora.Domain.Common;
using Domora.Domain.Common.Exceptions;
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
            throw new NotFoundException("Payment not found.");

        var invoice = await _invoiceRepository.GetByIdAsync(
            command.InvoiceId,
            cancellationToken
        );
        if (invoice is null)
            throw new NotFoundException("Invoice not found.");  
        
        
        var allocatedToPayment = await _paymentAllocationRepository.GetAllocatedAmountForPaymentAsync(
            command.PaymentId,
            cancellationToken
        );

        var allocatedToInvoice = await _paymentAllocationRepository.GetAllocatedAmountForInvoiceAsync(
            command.InvoiceId,
            cancellationToken
        );

        payment.EnsureCanAllocate(
            command.AllocateAmount,
            allocatedToPayment
        );

        var dynamicAllocationAmount = invoice.AllocatePayment(
            command.AllocateAmount,
            allocatedToInvoice 
        );

        var paymentAllocation = PaymentAllocation.Allocate(
            payment.Id,
            invoice.Id,
            new Money(dynamicAllocationAmount, command.AllocateAmount.Currency)
        );

        await _paymentAllocationRepository.AddAsync(
            paymentAllocation,
            cancellationToken
        );

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