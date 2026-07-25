using Domora.Domain.Units;
using Domora.Domain.Units.ValueObjects;

namespace Domora.Application.Units.Commands.RegisterUnit;

public sealed class RegisterUnitHandler
{
    private readonly IUnitRepository _unitRepository;

    public RegisterUnitHandler(IUnitRepository unitRepository)
    {
        _unitRepository = unitRepository;
    }

    public async Task<RegisterUnitResponse> Handle(RegisterUnitCommand command, CancellationToken cancellationToken)
    {
        var unitNumber = UnitNumber.Create(command.Number);

        var unit = Unit.Register(command.PropertyId, unitNumber, command.Type);

        await _unitRepository.AddAsync(unit, cancellationToken);

        return new RegisterUnitResponse(unit.Id, unit.PropertyId, unit.Number.Value, unit.Type, unit.Status);
    }
}