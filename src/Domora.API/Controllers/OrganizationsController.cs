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

    /// <summary>Creates an organization and makes the authenticated user its Owner.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(RegisterOrganizationResponse), StatusCodes.Status201Created)]
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

    /// <summary>Transfers organization ownership to an existing member.</summary>
    [RequireOrganizationAccess]
    [HttpPost("ownership/transfer")]
    [ProducesResponseType(typeof(TransferOwnershipResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> TransferOwnership(
        TransferOwnershipRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _transferOwnershipHandler.Handle(
            new TransferOwnershipCommand(request.NewOwnerUserId),
            cancellationToken);

        return Ok(response);
    }

    /// <summary>Adds an existing user to the current organization.</summary>
    [RequireOrganizationAccess]
    [HttpPost("memberships")]
    [ProducesResponseType(typeof(AddOrganizationMemberResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> AddMember(
        AddOrganizationMemberRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _addMemberHandler.Handle(
            new AddOrganizationMemberCommand(request.UserId, request.Role),
            cancellationToken);

        return Ok(response);
    }

    /// <summary>Changes a non-Owner member's role.</summary>
    [RequireOrganizationAccess]
    [HttpPut("memberships/{userId:guid}/role")]
    [ProducesResponseType(typeof(ChangeOrganizationMemberRoleResponse), StatusCodes.Status200OK)]
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

    /// <summary>Removes a non-Owner member from the current organization.</summary>
    [RequireOrganizationAccess]
    [HttpDelete("memberships/{userId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RemoveMember(
        Guid userId,
        CancellationToken cancellationToken)
    {
        await _removeMemberHandler.Handle(
            new RemoveOrganizationMemberCommand(userId),
            cancellationToken);

        return NoContent();
    }

    /// <summary>Leaves the current organization as a non-Owner member.</summary>
    [RequireOrganizationAccess]
    [HttpPost("memberships/leave")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> LeaveOrganization(
        CancellationToken cancellationToken)
    {
        await _leaveOrganizationHandler.Handle(
            new LeaveOrganizationCommand(),
            cancellationToken);

        return NoContent();
    }
}
