using Domora.Application.Common.Persistence;
using Domora.Domain.Leasing;
using Domora.Domain.Units;

namespace Domora.Application.Leasing.Commands.RegisterLease;

public sealed class RegisterLeaseHandler
{
    private readonly ILeaseRepository _leaseRepository;
    
    private readonly IUnitRepository _unitRepository;

    private readonly IUnitOfWork _unitOfWork;


    public RegisterLeaseHandler(
        ILeaseRepository leaseRepository,
        IUnitRepository unitRepository,
        IUnitOfWork unitOfWork
        )
    {
        _leaseRepository = leaseRepository;
        _unitRepository = unitRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<RegisterLeaseResponse> Handle(
        RegisterLeaseCommand command, 
        CancellationToken cancellationToken
        )
    {
        var unit = await _unitRepository.GetByIdAsync(
            command.UnitId,
            cancellationToken
        );

        if (unit is null)
            throw new InvalidOperationException("Unit not found.");

        unit.Occupy(); 

        var lease = Lease.Register(
            unit.Id,
            command.TenantId,
            command.StartDate,
            command.MonthlyRent
        );

        await _leaseRepository.AddAsync(
            lease, 
            cancellationToken
        );

        await _unitRepository.UpdateAsync(
            unit,
            cancellationToken
        );

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new RegisterLeaseResponse(
            lease.Id,
            lease.UnitId,
            lease.TenantId,
            lease.StartDate,
            lease.MonthlyRent.Amount,
            lease.MonthlyRent.Currency
        );
    } 
}