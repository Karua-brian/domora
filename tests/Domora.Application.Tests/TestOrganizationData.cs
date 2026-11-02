using Domora.Domain.Organizations;
using Domora.Domain.Organizations.Enums;
using Domora.Domain.Users;
using Domora.Domain.Users.ValueObjects;
using Domora.Infrastructure.Persistence;

namespace Domora.Application.Tests;

internal static class TestOrganizationData
{
    public static async Task AddOwnerAsync(
        DomoraDbContext context,
        Organization organization
    )
    {
        var user = User.Register(
            UserEmail.Create($"owner-{Guid.NewGuid():N}@example.com"),
            UserPassword.Create("Test-password-hash")
        );
        var membership = OrganizationMembership.Create(
            user.Id,
            organization.Id,
            OrganizationRole.Owner
        );

        await context.Users.AddAsync(user);
        await context.OrganizationMemberships.AddAsync(membership);
    }
}
