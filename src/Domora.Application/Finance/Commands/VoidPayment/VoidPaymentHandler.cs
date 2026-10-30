using Domora.Application.Common.Context;
using Domora.Application.Common.Exceptions;
using Domora.Application.Common.Persistence;
using Domora.Domain.Common.Exceptions;
using Domora.Domain.Finance;
using Domora.Domain.Finance.Enums;

namespace Domora.Application.Finance.Commands.VoidPayment;

public sealed class VoidPaymentHandler
{
    private readonly IOrganizationContext _organizationContext;
    private readonly IPaymentRepository _paymentRepository;
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly IPaymentAllocationRepository _paymentAllocationRepository;
    private readonly IPaymentVoidRepository _paymentVoidRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserContext _userContext;

    public VoidPaymentHandler(
        IOrganizationContext organizationContext,
        IPaymentRepository paymentRepository,
        IInvoiceRepository invoiceRepository,
        IPaymentAllocationRepository paymentAllocationRepository,
        IPaymentVoidRepository paymentVoidRepository,
        IUserContext userContext,
        IUnitOfWork unitOfWork
    )
    {
        _organizationContext = organizationContext;
        _paymentRepository = paymentRepository;
        _invoiceRepository = invoiceRepository;
        _paymentAllocationRepository = paymentAllocationRepository;
        _paymentVoidRepository = paymentVoidRepository;
        _userContext = userContext;
        _unitOfWork = unitOfWork;
    }

    public async Task<VoidPaymentResponse> Handle(
        VoidPaymentCommand command,
        CancellationToken cancellationToken
    )
    {
        if (string.IsNullOrWhiteSpace(command.Reason))
            throw new DomainValidationException("A payment void reason is required.");

        await using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);

        var payment = await _paymentRepository.GetByIdAsync(
            command.PaymentId,
            cancellationToken
        );
        if (payment is null)
            throw new NotFoundException(
                "Payment entry not found."
            );

        if (payment.OrganizationId != _organizationContext.OrganizationId)
            throw new NotFoundException(
                "Payment entry not found."
            );

        if (payment.Status == PaymentStatus.Voided)
            throw new ResourceConflictException("This payment has already been voided.");

        var linkedAllocations = await _paymentAllocationRepository
            .GetAllocationsByPaymentIdAsync(
                payment.Id,
                cancellationToken
            );
        
        foreach (var invoiceId in linkedAllocations.Select(x => x.InvoiceId).Distinct())
        {
            var invoice = await _invoiceRepository.GetByIdAsync(
                invoiceId,
                cancellationToken
            );
            if (invoice is null)
                throw new NotFoundException("Invoice not found.");

            var effectiveAllocationTotal = await _paymentAllocationRepository
                .GetAllocatedAmountForInvoiceAsync(
                    invoice.Id,
                    invoice.Amount.Currency,
                    cancellationToken,
                    excludedPaymentId: payment.Id
                );

            invoice.RecalculatePaymentStatus(effectiveAllocationTotal);
        }

        payment.Void();
        await _paymentVoidRepository.AddAsync(
            PaymentVoid.Create(
                payment.OrganizationId,
                payment.Id,
                _userContext.UserId,
                command.Reason),
            cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new VoidPaymentResponse(
            payment.Id,
            payment.Status.ToString()
        );
    }
}
