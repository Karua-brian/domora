using Domora.Application.Common.Context;
using Domora.Application.Common.Persistence;
using Domora.Domain.Common.Exceptions;
using Domora.Domain.Finance;
using Domora.Domain.Finance.Enums;
using Microsoft.Extensions.Logging;

namespace Domora.Application.Finance.Commands.ReceivePayment;

public sealed class ReceivePaymentHandler
{
    private readonly IOrganizationContext _organizationContext;
    private readonly IPaymentRepository _paymentRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ReceivePaymentHandler> _logger;

    public ReceivePaymentHandler(
        IOrganizationContext organizationContext,
        IPaymentRepository paymentRepository,
        IUnitOfWork unitOfWork,
        ILogger<ReceivePaymentHandler> logger
    )
    {
        _organizationContext = organizationContext;
        _paymentRepository = paymentRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ReceivePaymentResponse> Handle(
        ReceivePaymentCommand command,
        CancellationToken cancellationToken
    )
    {
        var organizationId = _organizationContext.OrganizationId;

        try 
        {
            await using var transaction = 
                await _unitOfWork.BeginTransactionAsync(cancellationToken);

            var existingPayment = await _paymentRepository.GetByReferenceAsync(
                organizationId,
                command.Reference,
                cancellationToken
            );

            // A retry of an already successful request returns the original payment
            if (existingPayment is not null)
            {
                if (existingPayment.Status == PaymentStatus.Voided)
                    throw new ResourceConflictException(
                        "A voided payment reference cannot be reused."
                    );

                await transaction.CommitAsync(cancellationToken);

                return new ReceivePaymentResponse(
                    existingPayment.Id,
                    existingPayment.TotalAmount.Amount,
                    existingPayment.TotalAmount.Currency,
                    existingPayment.PaidAt,
                    existingPayment.Reference,
                    existingPayment.Version
                );
            }

            var payment = Payment.Receive(
                organizationId,
                command.Amount,
                command.Reference
            );

            await _paymentRepository.AddAsync(
                payment,
                cancellationToken
            );

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            return new ReceivePaymentResponse(
                payment.Id,
                payment.TotalAmount.Amount,
                payment.TotalAmount.Currency,
                payment.PaidAt,
                payment.Reference,
                payment.Version
            );
        }
        catch (ResourceConflictException)
        {
            // A concurrent request may have inserted the same reference
            await using var recoveryTransaction = 
                await _unitOfWork.BeginTransactionAsync(cancellationToken);

            var existingPayment = await _paymentRepository.GetByReferenceAsync(
                organizationId,
                command.Reference,
                cancellationToken
            );

            if (existingPayment is null)
            {
                // The conflict was unrelated to this reference
                throw;
            }

            if (existingPayment.Status == PaymentStatus.Voided)
                throw new ResourceConflictException(
                    "A voided payment reference cannot be reused."
                );

            await recoveryTransaction.CommitAsync(cancellationToken);

            return new ReceivePaymentResponse(
                existingPayment.Id,
                existingPayment.TotalAmount.Amount,
                existingPayment.TotalAmount.Currency,
                existingPayment.PaidAt,
                existingPayment.Reference,
                existingPayment.Version
            );
        }
    }
}
