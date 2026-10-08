using Domora.Domain.Organizations;
using Domora.Domain.Organizations.Enums;
using Domora.Domain.Organizations.ValueObjects;
using Domora.Domain.Users;
using Domora.Domain.Users.ValueObjects;
using Domora.Infrastructure.Persistence;

namespace Domora.API.Tests.Infrastructure;

public static class TestData
{
    public static async Task<User> CreateUserAsync(
        DomoraDbContext context
    )
    {
        var user = User.Register(
            UserEmail.Create($"test-{Guid.NewGuid():N}@.com"),
            UserPassword.Create("Test-password-hash")
        );

        await context.Users.AddAsync(user);

        await context.SaveChangesAsync();

        return user;
    }

    public static async Task<Organization>
        CreateOrganizationAsync(
            DomoraDbContext context)
    {
        var organization =
            Organization.Register(
                OrganizationName.Create(
                    $"Test Org {Guid.NewGuid():N}"
                )
            );

        await context.Organizations.AddAsync(
            organization
        );

        await context.SaveChangesAsync();

        return organization;
    }

    public static async Task<OrganizationMembership>
        AddMembershipAsync(
            DomoraDbContext context,
            Guid userId,
            Guid organizationId)
    {
        var membership =
            OrganizationMembership.Create(
                userId,
                organizationId,
                OrganizationRole.Owner
            );

        await context.OrganizationMemberships.AddAsync(
            membership
        );

        await context.SaveChangesAsync();

        return membership;
    }
}