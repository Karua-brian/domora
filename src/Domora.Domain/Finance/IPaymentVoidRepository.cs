namespace Domora.Domain.Finance;

public interface IPaymentVoidRepository
{
    Task AddAsync(
        PaymentVoid paymentVoid, 
        CancellationToken cancellationToken = default
    );
}
