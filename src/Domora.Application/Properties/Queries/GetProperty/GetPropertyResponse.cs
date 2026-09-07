namespace Domora.Application.Properties.Queries.GetProperty;

public sealed record GetPropertyResponse(
    Guid Id,
    Guid OrganizationId,
    string Name
);