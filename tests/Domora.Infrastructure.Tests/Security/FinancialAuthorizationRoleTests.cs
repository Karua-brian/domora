using Domora.Application.Common.Authorization;
using Domora.Application.Common.Context;
using Domora.Application.Common.Exceptions;
using Domora.Domain.Common;
using Domora.Domain.Organizations;
using Domora.Domain.Organizations.Enums;
using Domora.Domain.Organizations.ValueObjects;
using Domora.Domain.Users;
using Domora.Domain.Users.ValueObjects;
using Domora.Infrastructure.Persistence;
using Domora.Infrastructure.Persistence.Interceptors;
using Domora.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Domora.Infrastructure.Tests.Security;

public sealed class FinancialAuthorizationRoleTests
{
    private sealed class TestContext(Guid organizationId) : IOrganizationContext, IUserContext
    {
        public Guid OrganizationId { get; private set; } = organizationId;
        public Guid UserId { get; init; }
        public void Set(Guid value) => OrganizationId = value;
    }

    [Fact]
    public async Task Unknown_persisted_role_fails_closed_for_financial_authorization()
    {
        var connectionString = Environment.GetEnvironmentVariable("DomoraTest")
            ?? throw new InvalidOperationException("DomoraTest connection string is not configured.");
        var organization = Organization.Register(
            OrganizationName.Create($"Invalid role test {Guid.NewGuid():N}"));
        var user = User.Register(
            UserEmail.Create($"invalid-role-{Guid.NewGuid():N}@example.com"),
            UserPassword.Create("Hashed-password"));
        var context = new TestContext(organization.Id) { UserId = user.Id };
        var options = new DbContextOptionsBuilder<DomoraDbContext>()
            .UseNpgsql(connectionString)
            .AddInterceptors(new OrganizationTransactionInterceptor(context))
            .Options;

        await using var db = new DomoraDbContext(options);
        await using var transaction = await db.Database.BeginTransactionAsync();
        await db.Users.AddAsync(user);
        await db.Organizations.AddAsync(organization);
        await db.SaveChangesAsync();

        await db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO "OrganizationMemberships" ("Id", "UserId", "OrganizationId", "Role")
            VALUES ({Guid.NewGuid()}, {user.Id}, {organization.Id}, {"UnrecognizedRole"})
            """);

        var memberships = new OrganizationMembershipRepository(db);
        Assert.False(await memberships.HasRoleAsync(
            user.Id, organization.Id, OrganizationRole.Owner, CancellationToken.None));
        Assert.False(await memberships.HasRoleAsync(
            user.Id, organization.Id, OrganizationRole.Finance, CancellationToken.None));

        var authorization = new FinancialAuthorization(context, context, memberships);
        await Assert.ThrowsAsync<ForbiddenException>(() =>
            authorization.EnsureCanAsync(FinancialCapability.ReceivePayments));
    }
}
