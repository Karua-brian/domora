using Domora.Domain.Units.Enums;

namespace Domora.API.Units;

public sealed record RegisterUnitRequest(Guid PropertyId, string Number, UnitType Type);