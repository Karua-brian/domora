using Domora.Application.Common.Context;
using Domora.Application.Common.Persistence;
using Domora.Domain.Finance;

namespace Domora.Application.Finance.Commands.ReceivePayment;

public sealed class ReceivePaymentHandler
{
    private readonly IOrganizationContext _organizationContext;
    private readonly IPaymentRepository _paymentRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ReceivePaymentHandler(
        IOrganizationContext organizationContext,
        IPaymentRepository paymentRepository,
        IUnitOfWork unitOfWork
    )
    {
        _organizationContext = organizationContext;
        _paymentRepository = paymentRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<ReceivePaymentResponse> Handle(
        ReceivePaymentCommand command,
        CancellationToken cancellationToken
    )
    {
        var existingPayment = await _paymentRepository.GetByReferenceAsync(
            _organizationContext.OrganizationId,
            command.Reference,
            cancellationToken
        );

        if (existingPayment is not null)
        {
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
            _organizationContext.OrganizationId,
            command.Amount,
            command.Reference
        );

        await _paymentRepository.AddAsync(
            payment,
            cancellationToken
        );

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new ReceivePaymentResponse(
            payment.Id,
            payment.TotalAmount.Amount,
            payment.TotalAmount.Currency,
            payment.PaidAt,
            payment.Reference,
            payment.Version
        );
    }
}