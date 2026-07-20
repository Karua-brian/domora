using Domora.Domain.Organizations.ValueObjects;

namespace Domora.Application.Organizations.Commands.RegisterOrganization;

public sealed record RegisterOrganizationCommand(OrganizationName Name);

