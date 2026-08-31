using Domora.Application.Common.Exceptions;
using Domora.Application.Common.Persistence;
using Domora.Domain.Leasing;
using Domora.Domain.Units;

namespace Domora.Application.Leasing.Commands.EndLease;

public sealed class EndLeaseHandler
{
    private readonly ILeaseRepository _leaseRepository;

    private readonly IUnitRepository _unitRepository;

    private readonly IUnitOfWork _unitOfWork;

    public EndLeaseHandler(
        ILeaseRepository leaseRepository,
        IUnitRepository unitRepository,
        IUnitOfWork unitOfWork
    )
    {
        _leaseRepository = leaseRepository;
        _unitRepository = unitRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<EndLeaseResponse> Handle(
        EndLeaseCommand command,
        CancellationToken cancellationToken
    )
    {
        var lease = await _leaseRepository.GetByIdAsync(
            command.LeaseId,
            cancellationToken
        );

        if (lease is null)
            throw new NotFoundException(
                "Lease not found."
            );

        var unit = await _unitRepository.GetByIdAsync(
            lease.UnitId,
            cancellationToken
        ) ?? throw new NotFoundException(
                "Unit not found."
            );

        lease.EndLease(command.EndDate);
        unit.Vacate();

        await _unitRepository.UpdateAsync(
            unit,
            cancellationToken
        );

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new EndLeaseResponse(
            lease.Id,
            lease.UnitId,
            lease.TenantId,
            lease.StartDate,
            lease.EndDate!.Value,
            lease.Status,
            lease.Version
        );
    }
}