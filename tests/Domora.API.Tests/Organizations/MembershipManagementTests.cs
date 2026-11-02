using System.Net;
using System.Net.Http.Json;
using Domora.API.Tests.Infrastructure;
using Domora.Domain.Organizations.Enums;
using Domora.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Domora.API.Tests.Organizations;

[Collection("Domora API database")]
public sealed class MembershipManagementTests : IClassFixture<DomoraWebApplicationFactory>
{
    private readonly DomoraWebApplicationFactory _factory;

    public MembershipManagementTests(DomoraWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Owner_can_add_existing_user_and_change_nonowner_role()
    {
        var seeded = await SeedAsync();
        using var owner = CreateClient(seeded.OwnerId, seeded.OrganizationId);

        var add = await AddMemberAsync(owner, seeded.NewUserId, OrganizationRole.Finance);
        Assert.Equal(HttpStatusCode.OK, add.StatusCode);
        await AssertRoleAsync(seeded.OrganizationId, seeded.NewUserId, OrganizationRole.Finance);

        var addAsMember = await AddMemberAsync(owner, seeded.AdditionalUserId, OrganizationRole.Member);
        Assert.Equal(HttpStatusCode.OK, addAsMember.StatusCode);
        await AssertRoleAsync(seeded.OrganizationId, seeded.AdditionalUserId, OrganizationRole.Member);

        var changeToMember = await ChangeRoleAsync(
            owner, seeded.NewUserId, OrganizationRole.Member);
        Assert.Equal(HttpStatusCode.OK, changeToMember.StatusCode);
        await AssertRoleAsync(seeded.OrganizationId, seeded.NewUserId, OrganizationRole.Member);

        var changeToFinance = await ChangeRoleAsync(
            owner, seeded.NewUserId, OrganizationRole.Finance);
        Assert.Equal(HttpStatusCode.OK, changeToFinance.StatusCode);
        await AssertRoleAsync(seeded.OrganizationId, seeded.NewUserId, OrganizationRole.Finance);
    }

    [Fact]
    public async Task Nonowner_cannot_add_or_change_members_and_owner_cannot_assign_owner_directly()
    {
        var seeded = await SeedAsync();
        using var finance = CreateClient(seeded.FinanceId, seeded.OrganizationId);
        using var owner = CreateClient(seeded.OwnerId, seeded.OrganizationId);

        Assert.Equal(HttpStatusCode.Forbidden,
            (await AddMemberAsync(finance, seeded.NewUserId, OrganizationRole.Member)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await ChangeRoleAsync(finance, seeded.MemberId, OrganizationRole.Finance)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest,
            (await AddMemberAsync(owner, seeded.NewUserId, OrganizationRole.Owner)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest,
            (await ChangeRoleAsync(owner, seeded.MemberId, OrganizationRole.Owner)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict,
            (await ChangeRoleAsync(owner, seeded.OwnerId, OrganizationRole.Member)).StatusCode);
    }

    [Fact]
    public async Task Duplicate_membership_and_invalid_targets_are_rejected()
    {
        var seeded = await SeedAsync();
        using var owner = CreateClient(seeded.OwnerId, seeded.OrganizationId);

        Assert.Equal(HttpStatusCode.Conflict,
            (await AddMemberAsync(owner, seeded.MemberId, OrganizationRole.Finance)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,
            (await AddMemberAsync(owner, Guid.NewGuid(), OrganizationRole.Member)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,
            (await ChangeRoleAsync(owner, seeded.NewUserId, OrganizationRole.Finance)).StatusCode);

        await AssertRoleAsync(seeded.OrganizationId, seeded.MemberId, OrganizationRole.Member);
    }

    [Fact]
    public async Task Concurrent_adds_preserve_one_membership_for_the_user()
    {
        var seeded = await SeedAsync();
        using var ownerA = CreateClient(seeded.OwnerId, seeded.OrganizationId);
        using var ownerB = CreateClient(seeded.OwnerId, seeded.OrganizationId);

        var responses = await Task.WhenAll(
            AddMemberAsync(ownerA, seeded.NewUserId, OrganizationRole.Member),
            AddMemberAsync(ownerB, seeded.NewUserId, OrganizationRole.Finance));

        Assert.Single(responses, x => x.StatusCode == HttpStatusCode.OK);
        Assert.Single(responses, x => x.StatusCode == HttpStatusCode.Conflict);

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<DomoraDbContext>();
        Assert.Equal(1, await db.OrganizationMemberships.CountAsync(x =>
            x.OrganizationId == seeded.OrganizationId && x.UserId == seeded.NewUserId));
        Assert.Equal(1, await db.OrganizationMemberships.CountAsync(x =>
            x.OrganizationId == seeded.OrganizationId && x.Role == OrganizationRole.Owner));
    }

    private async Task<SeededMemberships> SeedAsync()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<DomoraDbContext>();
        var owner = await TestData.CreateUserAsync(db);
        var finance = await TestData.CreateUserAsync(db);
        var member = await TestData.CreateUserAsync(db);
        var newUser = await TestData.CreateUserAsync(db);
        var additionalUser = await TestData.CreateUserAsync(db);
        var organization = await TestData.CreateOrganizationAsync(db);
        await TestData.AddMembershipAsync(db, owner.Id, organization.Id, OrganizationRole.Owner);
        await TestData.AddMembershipAsync(db, finance.Id, organization.Id, OrganizationRole.Finance);
        await TestData.AddMembershipAsync(db, member.Id, organization.Id, OrganizationRole.Member);
        return new SeededMemberships(
            organization.Id, owner.Id, finance.Id, member.Id, newUser.Id, additionalUser.Id);
    }

    private HttpClient CreateClient(Guid userId, Guid organizationId)
    {
        var client = AuthenticatedClientFactory.Create(_factory, userId);
        client.DefaultRequestHeaders.Add("X-Organization-Id", organizationId.ToString());
        return client;
    }

    private static Task<HttpResponseMessage> AddMemberAsync(
        HttpClient client,
        Guid userId,
        OrganizationRole role) =>
        client.PostAsJsonAsync(
            "/api/organizations/memberships",
            new { userId, role = role.ToString() });

    private static Task<HttpResponseMessage> ChangeRoleAsync(
        HttpClient client,
        Guid userId,
        OrganizationRole role) =>
        client.PutAsJsonAsync(
            $"/api/organizations/memberships/{userId}/role",
            new { role = role.ToString() });

    private async Task AssertRoleAsync(
        Guid organizationId,
        Guid userId,
        OrganizationRole role)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<DomoraDbContext>();
        var membership = await db.OrganizationMemberships.SingleAsync(x =>
            x.OrganizationId == organizationId && x.UserId == userId);
        Assert.Equal(role, membership.Role);
    }

    private sealed record SeededMemberships(
        Guid OrganizationId,
        Guid OwnerId,
        Guid FinanceId,
        Guid MemberId,
        Guid NewUserId,
        Guid AdditionalUserId);
}
