using Domora.Application.Common.Persistence;
using Domora.Domain.Finance;

namespace Domora.Application.Finance.Commands.ReceivePayment;

public sealed class ReceivePaymentHandler
{
    private readonly IPaymentRepository _paymentRepository;

    private readonly IUnitOfWork _unitOfWork;

    public ReceivePaymentHandler(
        IPaymentRepository paymentRepository,
        IUnitOfWork unitOfWork
    )
    {
        _paymentRepository = paymentRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<ReceivePaymentResponse> Handle(
        ReceivePaymentCommand command,
        CancellationToken cancellationToken
    )
    {
        var existingPayment = await _paymentRepository.GetByReferenceAsync(
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