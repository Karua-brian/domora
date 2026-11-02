using System.Net;
using System.Net.Http.Json;
using Domora.API.Tests.Infrastructure;
using Domora.Domain.Organizations.Enums;
using Domora.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Domora.API.Tests.Organizations;

[Collection("Domora API database")]
public sealed class OwnershipTransferTests : IClassFixture<DomoraWebApplicationFactory>
{
    private readonly DomoraWebApplicationFactory _factory;

    public OwnershipTransferTests(DomoraWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Owner_can_transfer_ownership_and_former_owner_becomes_finance()
    {
        var (organizationId, ownerId, targetId, _) = await SeedOrganizationAsync();
        using var ownerClient = CreateClient(ownerId, organizationId);

        var response = await TransferAsync(ownerClient, targetId);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await AssertSingleOwnerAsync(organizationId, targetId, ownerId, OrganizationRole.Finance);

        using var newOwnerClient = CreateClient(targetId, organizationId);
        var selfTransfer = await TransferAsync(newOwnerClient, targetId);
        Assert.Equal(HttpStatusCode.Conflict, selfTransfer.StatusCode);
    }

    [Fact]
    public async Task Nonowner_cannot_transfer_and_nonmember_cannot_become_owner()
    {
        var (organizationId, ownerId, targetId, memberId) = await SeedOrganizationAsync();
        using var memberClient = CreateClient(memberId, organizationId);
        using var ownerClient = CreateClient(ownerId, organizationId);
        var outsiderId = Guid.NewGuid();

        Assert.Equal(HttpStatusCode.Forbidden,
            (await TransferAsync(memberClient, targetId)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,
            (await TransferAsync(ownerClient, outsiderId)).StatusCode);
        await AssertSingleOwnerAsync(organizationId, ownerId, targetId, OrganizationRole.Finance);
    }

    [Fact]
    public async Task Concurrent_transfers_leave_exactly_one_owner()
    {
        var (organizationId, ownerId, targetId, memberId) = await SeedOrganizationAsync();
        using var ownerClientA = CreateClient(ownerId, organizationId);
        using var ownerClientB = CreateClient(ownerId, organizationId);

        var responses = await Task.WhenAll(
            TransferAsync(ownerClientA, targetId),
            TransferAsync(ownerClientB, memberId));

        Assert.Single(responses, x => x.StatusCode == HttpStatusCode.OK);
        Assert.Single(responses, x => x.StatusCode == HttpStatusCode.Forbidden);

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<DomoraDbContext>();
        var memberships = await db.OrganizationMemberships
            .Where(x => x.OrganizationId == organizationId)
            .ToListAsync();
        Assert.Single(memberships, x => x.Role == OrganizationRole.Owner);
    }

    private async Task<(Guid OrganizationId, Guid OwnerId, Guid TargetId, Guid MemberId)>
        SeedOrganizationAsync()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<DomoraDbContext>();
        var owner = await TestData.CreateUserAsync(db);
        var target = await TestData.CreateUserAsync(db);
        var member = await TestData.CreateUserAsync(db);
        var organization = await TestData.CreateOrganizationAsync(db);
        await TestData.AddMembershipAsync(db, owner.Id, organization.Id, OrganizationRole.Owner);
        await TestData.AddMembershipAsync(db, target.Id, organization.Id, OrganizationRole.Finance);
        await TestData.AddMembershipAsync(db, member.Id, organization.Id, OrganizationRole.Member);
        return (organization.Id, owner.Id, target.Id, member.Id);
    }

    private HttpClient CreateClient(Guid userId, Guid organizationId)
    {
        var client = AuthenticatedClientFactory.Create(_factory, userId);
        client.DefaultRequestHeaders.Add("X-Organization-Id", organizationId.ToString());
        return client;
    }

    private static Task<HttpResponseMessage> TransferAsync(HttpClient client, Guid newOwnerUserId) =>
        client.PostAsJsonAsync("/api/organizations/ownership/transfer", new { newOwnerUserId });

    private async Task AssertSingleOwnerAsync(
        Guid organizationId,
        Guid ownerId,
        Guid formerOwnerId,
        OrganizationRole formerOwnerRole)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<DomoraDbContext>();
        var memberships = await db.OrganizationMemberships
            .Where(x => x.OrganizationId == organizationId)
            .ToListAsync();
        Assert.Single(memberships, x => x.Role == OrganizationRole.Owner && x.UserId == ownerId);
        Assert.Equal(formerOwnerRole,
            memberships.Single(x => x.UserId == formerOwnerId).Role);
        Assert.Single(memberships, x => x.Role == OrganizationRole.Owner);
    }
}
