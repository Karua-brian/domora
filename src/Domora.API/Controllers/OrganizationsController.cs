using Microsoft.AspNetCore.Mvc;
using Domora.Application.Organizations.Commands.RegisterOrganization;
using Domora.Application.Organizations.Commands.TransferOwnership;
using Domora.Application.Organizations.Commands.AddOrganizationMember;
using Domora.Application.Organizations.Commands.ChangeOrganizationMemberRole;
using Domora.Application.Organizations.Commands.RemoveOrganizationMember;
using Domora.Application.Organizations.Commands.LeaveOrganization;
using Domora.API.Organizations;
using Domora.API.Common.Authorization;

namespace Domora.API.Controllers;

[ApiController]
[Route("api/Organizations")]
public sealed class OrganizationsController : ControllerBase
{
    private readonly RegisterOrganizationHandler _handler;
    private readonly TransferOwnershipHandler _transferOwnershipHandler;
    private readonly AddOrganizationMemberHandler _addMemberHandler;
    private readonly ChangeOrganizationMemberRoleHandler _changeMemberRoleHandler;
    private readonly RemoveOrganizationMemberHandler _removeMemberHandler;
    private readonly LeaveOrganizationHandler _leaveOrganizationHandler;

    public OrganizationsController(
        RegisterOrganizationHandler handler,
        TransferOwnershipHandler transferOwnershipHandler,
        AddOrganizationMemberHandler addMemberHandler,
        ChangeOrganizationMemberRoleHandler changeMemberRoleHandler,
        RemoveOrganizationMemberHandler removeMemberHandler,
        LeaveOrganizationHandler leaveOrganizationHandler
    )
    {
        _handler = handler;
        _transferOwnershipHandler = transferOwnershipHandler;
        _addMemberHandler = addMemberHandler;
        _changeMemberRoleHandler = changeMemberRoleHandler;
        _removeMemberHandler = removeMemberHandler;
        _leaveOrganizationHandler = leaveOrganizationHandler;
    }

    [HttpPost]
    public async Task<IActionResult> Register(
        RegisterOrganizationRequest request,
        CancellationToken cancellationToken
        )
    {
        var command = new RegisterOrganizationCommand(
            request.Name
        );

        var response = await _handler.Handle(
            command, 
            cancellationToken
        );

        return Created(
            $"/api/organizations/{response.Id}", 
            response
        );
    }

    [RequireOrganizationAccess]
    [HttpPost("ownership/transfer")]
    public async Task<IActionResult> TransferOwnership(
        TransferOwnershipRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _transferOwnershipHandler.Handle(
            new TransferOwnershipCommand(request.NewOwnerUserId),
            cancellationToken);

        return Ok(response);
    }

    [RequireOrganizationAccess]
    [HttpPost("memberships")]
    public async Task<IActionResult> AddMember(
        AddOrganizationMemberRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _addMemberHandler.Handle(
            new AddOrganizationMemberCommand(request.UserId, request.Role),
            cancellationToken);

        return Ok(response);
    }

    [RequireOrganizationAccess]
    [HttpPut("memberships/{userId:guid}/role")]
    public async Task<IActionResult> ChangeMemberRole(
        Guid userId,
        ChangeOrganizationMemberRoleRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _changeMemberRoleHandler.Handle(
            new ChangeOrganizationMemberRoleCommand(userId, request.Role),
            cancellationToken);

        return Ok(response);
    }

    [RequireOrganizationAccess]
    [HttpDelete("memberships/{userId:guid}")]
    public async Task<IActionResult> RemoveMember(
        Guid userId,
        CancellationToken cancellationToken)
    {
        await _removeMemberHandler.Handle(
            new RemoveOrganizationMemberCommand(userId),
            cancellationToken);

        return NoContent();
    }

    [RequireOrganizationAccess]
    [HttpPost("memberships/leave")]
    public async Task<IActionResult> LeaveOrganization(
        CancellationToken cancellationToken)
    {
        await _leaveOrganizationHandler.Handle(
            new LeaveOrganizationCommand(),
            cancellationToken);

        return NoContent();
    }
}
