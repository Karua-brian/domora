using Domora.Application.Common.Persistence;
using Domora.Domain.Units;
using Domora.Domain.Units.ValueObjects;

namespace Domora.Application.Units.Commands.RegisterUnit;

public sealed class RegisterUnitHandler
{
    private readonly IUnitRepository _unitRepository;

    private readonly IUnitOfWork _unitOfWork;

    public RegisterUnitHandler(
        IUnitRepository unitRepository,
        IUnitOfWork unitOfWork
        )
    {
        _unitRepository = unitRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<RegisterUnitResponse> Handle(RegisterUnitCommand command, CancellationToken cancellationToken)
    {
        var unitNumber = UnitNumber.Create(command.Number);

        var unit = Unit.Register(command.PropertyId, unitNumber, command.Type);

        await _unitRepository.AddAsync(unit, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new RegisterUnitResponse(unit.Id, unit.PropertyId, unit.Number.Value, unit.Type, unit.Status);
    }
}