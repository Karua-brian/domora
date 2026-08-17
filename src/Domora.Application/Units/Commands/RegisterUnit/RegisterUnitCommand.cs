using Domora.Domain.Units.Enums;

namespace Domora.Application.Units.Commands.RegisterUnit;

public sealed record RegisterUnitCommand(
    Guid PropertyId, 
    string Number, 
    UnitType Type
    );