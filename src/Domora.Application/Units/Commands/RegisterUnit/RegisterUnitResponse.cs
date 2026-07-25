using Domora.Domain.Units.Enums;

namespace Domora.Application.Units.Commands.RegisterUnit;

public sealed record RegisterUnitResponse(Guid Id, Guid PropertyId, string Number, UnitType Type, OccupancyStatus Status);