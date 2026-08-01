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
        var paidAt = command.PaidAt.ToUniversalTime();

        var payment = Payment.Receive(
            command.Amount,
            paidAt,
            command.Reference
        );

        await _paymentRepository.AddAsync(
            payment,
            cancellationToken
        );

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new ReceivePaymentResponse(
            payment.Id,
            payment.Amount.Amount,
            payment.Amount.Currency,
            payment.PaidAt.ToUniversalTime(),
            payment.Reference
        );
    }
}