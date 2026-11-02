using Domora.Domain.Organizations;
using Domora.Domain.Organizations.Enums;
using Domora.Domain.Organizations.ValueObjects;
using Domora.Domain.Users;
using Domora.Domain.Users.ValueObjects;
using Domora.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Domora.Infrastructure.Tests.Persistence;

public sealed class OrganizationMembershipIntegrityTests
{
    private readonly string _connectionString =
        Environment.GetEnvironmentVariable("DomoraTest")
        ?? throw new InvalidOperationException(
            "DomoraTest connection string is not configured.");

    private DbContextOptions<DomoraDbContext> CreateOptions() =>
        new DbContextOptionsBuilder<DomoraDbContext>()
            .UseNpgsql(_connectionString)
            .Options;

    [Fact]
    public async Task Database_rejects_unsupported_role_values()
    {
        await using var db = new DomoraDbContext(CreateOptions());
        await using var transaction = await db.Database.BeginTransactionAsync();
        var (organization, owner) = await AddOrganizationWithOwnerAsync(db);
        var otherUser = NewUser();
        await db.Users.AddAsync(otherUser);
        await db.SaveChangesAsync();

        var exception = await Record.ExceptionAsync(() =>
            db.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO "OrganizationMemberships" ("Id", "UserId", "OrganizationId", "Role")
                VALUES ({Guid.NewGuid()}, {otherUser.Id}, {organization.Id}, {"UnrecognizedRole"})
                """));

        AssertConstraintViolation(exception, "23514", "CK_OrganizationMemberships_Role");
        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task Database_rejects_empty_role_values()
    {
        await using var db = new DomoraDbContext(CreateOptions());
        await using var transaction = await db.Database.BeginTransactionAsync();
        var (organization, owner) = await AddOrganizationWithOwnerAsync(db);
        var otherUser = NewUser();
        await db.Users.AddAsync(otherUser);
        await db.SaveChangesAsync();

        var exception = await Record.ExceptionAsync(() =>
            db.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO "OrganizationMemberships" ("Id", "UserId", "OrganizationId", "Role")
                VALUES ({Guid.NewGuid()}, {otherUser.Id}, {organization.Id}, {string.Empty})
                """));

        AssertConstraintViolation(exception, "23514", "CK_OrganizationMemberships_Role");
        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task Database_rejects_a_second_owner_for_the_same_organization()
    {
        await using var db = new DomoraDbContext(CreateOptions());
        await using var transaction = await db.Database.BeginTransactionAsync();
        var (organization, _) = await AddOrganizationWithOwnerAsync(db);
        var secondUser = NewUser();
        await db.Users.AddAsync(secondUser);
        await db.OrganizationMemberships.AddAsync(
            OrganizationMembership.Create(
                secondUser.Id,
                organization.Id,
                OrganizationRole.Owner));

        var exception = await Record.ExceptionAsync(() => db.SaveChangesAsync());

        AssertConstraintViolation(
            exception,
            "23505",
            "IX_OrganizationMemberships_OrganizationId_Owner");
        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task Separate_organizations_can_each_have_an_owner()
    {
        await using var db = new DomoraDbContext(CreateOptions());
        await using var transaction = await db.Database.BeginTransactionAsync();
        var (organizationA, _) = await AddOrganizationWithOwnerAsync(db);
        var (organizationB, _) = await AddOrganizationWithOwnerAsync(db);

        Assert.Equal(
            2,
            await db.OrganizationMemberships.CountAsync(
                membership => membership.Role == OrganizationRole.Owner &&
                    (membership.OrganizationId == organizationA.Id ||
                     membership.OrganizationId == organizationB.Id)));

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task Duplicate_user_organization_membership_remains_rejected()
    {
        await using var db = new DomoraDbContext(CreateOptions());
        await using var transaction = await db.Database.BeginTransactionAsync();
        var (organization, owner) = await AddOrganizationWithOwnerAsync(db);
        await db.OrganizationMemberships.AddAsync(
            OrganizationMembership.Create(
                owner.Id,
                organization.Id,
                OrganizationRole.Member));

        var exception = await Record.ExceptionAsync(() => db.SaveChangesAsync());

        AssertConstraintViolation(
            exception,
            "23505",
            "IX_OrganizationMemberships_UserId_OrganizationId");
        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task Failed_transfer_transaction_restores_original_owner()
    {
        await using var db = new DomoraDbContext(CreateOptions());
        var (organization, owner) = await AddOrganizationWithOwnerAsync(db);
        var target = NewUser();
        await db.Users.AddAsync(target);
        var targetMembership = OrganizationMembership.Create(
            target.Id,
            organization.Id,
            OrganizationRole.Finance);
        await db.OrganizationMemberships.AddAsync(targetMembership);
        await db.SaveChangesAsync();

        await using (var transfer = await db.Database.BeginTransactionAsync())
        {
            var trackedOwner = await db.OrganizationMemberships.SingleAsync(
                membership => membership.UserId == owner.Id &&
                    membership.OrganizationId == organization.Id);
            trackedOwner.DemoteForOwnershipTransferTo(targetMembership);
            await db.SaveChangesAsync();

            var exception = await Record.ExceptionAsync(() =>
                db.Database.ExecuteSqlInterpolatedAsync($"""
                    UPDATE "OrganizationMemberships"
                    SET "Role" = {"Unsupported"}
                    WHERE "Id" = {targetMembership.Id}
                    """));

            AssertConstraintViolation(exception, "23514", "CK_OrganizationMemberships_Role");
            await transfer.RollbackAsync();
        }

        db.ChangeTracker.Clear();
        Assert.True(await db.OrganizationMemberships.AnyAsync(
            membership => membership.OrganizationId == organization.Id &&
                membership.UserId == owner.Id &&
                membership.Role == OrganizationRole.Owner));
        Assert.False(await db.OrganizationMemberships.AnyAsync(
            membership => membership.OrganizationId == organization.Id &&
                membership.Role == OrganizationRole.Owner &&
                membership.UserId == target.Id));
    }

    private static async Task<(Organization Organization, User Owner)>
        AddOrganizationWithOwnerAsync(DomoraDbContext db)
    {
        var organization = Organization.Register(
            OrganizationName.Create($"Membership integrity {Guid.NewGuid():N}"));
        var owner = NewUser();
        await db.Organizations.AddAsync(organization);
        await db.Users.AddAsync(owner);
        await db.OrganizationMemberships.AddAsync(
            OrganizationMembership.Create(
                owner.Id,
                organization.Id,
                OrganizationRole.Owner));
        await db.SaveChangesAsync();
        return (organization, owner);
    }

    private static User NewUser() => User.Register(
        UserEmail.Create($"membership-{Guid.NewGuid():N}@example.com"),
        UserPassword.Create("Test-password-hash"));

    private static void AssertConstraintViolation(
        Exception? exception,
        string sqlState,
        string constraintName)
    {
        var postgresException = FindPostgresException(exception);
        Assert.NotNull(postgresException);
        Assert.Equal(sqlState, postgresException.SqlState);
        Assert.Equal(constraintName, postgresException.ConstraintName);
    }

    private static PostgresException? FindPostgresException(Exception? exception)
    {
        for (var current = exception; current is not null; current = current.InnerException)
        {
            if (current is PostgresException postgresException)
                return postgresException;
        }

        return null;
    }
}
