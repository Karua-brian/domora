using Domora.Domain.Finance;

namespace Domora.Application.Finance.Commands.ReceivePayment;

public sealed class ReceivePaymentHandler
{
    private readonly IPaymentRepository _paymentRepository;

    public ReceivePaymentHandler(
        IPaymentRepository paymentRepository
    )
    {
        _paymentRepository = paymentRepository;
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

        return new ReceivePaymentResponse(
            payment.Id,
            payment.Amount.Amount,
            payment.Amount.Currency,
            payment.PaidAt.ToUniversalTime(),
            payment.Reference
        );
    }
}