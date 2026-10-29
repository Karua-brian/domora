using Domora.Application.Common.Exceptions;
using Domora.Application.Common.Persistence;
using Domora.Domain.Common.Exceptions;
using Domora.Domain.Leasing;
using Domora.Domain.Leasing.Enums;
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
        await using var transaction = 
            await _unitOfWork.BeginTransactionAsync(
                cancellationToken
            );

        var unit = await _unitRepository.GetByIdAsync(
            command.UnitId,
            cancellationToken
        );
        if (unit is null)
            throw new NotFoundException(
                "Unit not found."
            );

        // Prevent overlapping double-leasing
        var isAlreadyLeased = await _leaseRepository.HasActiveLeaseAsync(
            unit.Id,
            cancellationToken
        );

        if (isAlreadyLeased)
            throw new ResourceConflictException(
                "Unit is already leased."
            );

        unit.Occupy(); 

        var lease = Lease.Register(
            unit.Id,
            command.TenantId,
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

        try 
        {
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (ResourceConflictException ex)
        {
            throw new ConcurrencyException(
                "The unit was leased by another operation before this registration completed.",
                ex
            );
        }

        await transaction.CommitAsync();

        return new RegisterLeaseResponse(
            lease.Id,
            lease.UnitId,
            lease.TenantId,
            lease.StartDate,
            lease.MonthlyRent.Amount,
            lease.MonthlyRent.Currency,
            lease.Status,
            lease.Version
        );

    } 
}