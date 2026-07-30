using Domora.Domain.Finance;

namespace Domora.Application.Finance.Commands.AllocatePayment;

public sealed class AllocatePaymentHandler
{
    private readonly IPaymentRepository _paymentRepository;

    private readonly IInvoiceRepository _invoiceRepository;

    private readonly IPaymentAllocationRepository _paymentAllocationRepository;

    public AllocatePaymentHandler(
        IPaymentRepository paymentRepository,
        IInvoiceRepository invoiceRepository,
        IPaymentAllocationRepository paymentAllocationRepository
    )
    {
        _paymentRepository = paymentRepository;
        _invoiceRepository = invoiceRepository;
        _paymentAllocationRepository = paymentAllocationRepository;
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

        var paymentAllocation = PaymentAllocation.Allocate(
            payment.Id,
            invoice.Id,
            command.AllocateAmount
        );

        await _paymentAllocationRepository.AddAsync(
            paymentAllocation,
            cancellationToken
        );

        invoice.MarkAsPaid();

        return new AllocatePaymentResponse(
            paymentAllocation.Id,
            paymentAllocation.PaymentId,
            paymentAllocation.InvoiceId,
            paymentAllocation.AllocateAmount.Amount,
            paymentAllocation.AllocateAmount.Currency
        );
    } 
}