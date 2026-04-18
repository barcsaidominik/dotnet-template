using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Template.Api.Contracts;
using Template.Api.Extensions;
using Template.Application.Admin.Commands.ApproveUser;
using Template.Application.Admin.Commands.CreateFacility;
using Template.Application.Admin.Commands.DeleteFacility;
using Template.Application.Admin.Commands.DeleteUser;
using Template.Application.Admin.Queries.GetAllFacilities;
using Template.Application.Admin.Queries.GetAllUsers;
using Template.Application.Common.Dtos;
using Template.Domain.Constants;

namespace Template.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = Roles.SystemAdmin)]
public sealed class AdminController(ISender sender) : ControllerBase
{
    private readonly ISender _sender = sender;

    [HttpGet("users")]
    [ProducesResponseType(typeof(IReadOnlyList<UserDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUsers(CancellationToken ct)
    {
        return (await _sender.Send(new GetAllUsersQuery(), ct)).ToActionResult();
    }

    [HttpPost("users/{userId:guid}/approve")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ApproveUser(Guid userId, [FromBody] ApproveUserRequest request, CancellationToken ct)
    {
        return (await _sender.Send(new ApproveUserCommand(userId, request.FacilityId, request.Role), ct)).ToActionResult();
    }

    [HttpDelete("users/{userId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteUser(Guid userId, CancellationToken ct)
    {
        return (await _sender.Send(new DeleteUserCommand(userId), ct)).ToActionResult();
    }

    [HttpGet("facilities")]
    [ProducesResponseType(typeof(IReadOnlyList<FacilityDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetFacilities(CancellationToken ct)
    {
        return (await _sender.Send(new GetAllFacilitiesQuery(), ct)).ToActionResult();
    }

    [HttpPost("facilities")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateFacility([FromBody] CreateFacilityCommand command, CancellationToken ct)
    {
        return (await _sender.Send(command, ct)).ToActionResult(id => CreatedAtAction(nameof(GetFacilities), new { id }, id));
    }

    [HttpDelete("facilities/{facilityId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteFacility(Guid facilityId, CancellationToken ct)
    {
        return (await _sender.Send(new DeleteFacilityCommand(facilityId), ct)).ToActionResult();
    }
}
