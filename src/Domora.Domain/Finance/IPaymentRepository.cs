namespace Domora.Domain.Finance;

public interface IPaymentRepository
{
    Payment? GetById(Guid id);

    void Add(Payment payment);

    void Update(Payment payment);
}