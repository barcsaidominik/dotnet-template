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

[Route("api/[controller]")]
[Authorize(Roles = Roles.SYSTEM_ADMIN)]
public sealed class AdminController(ISender sender) : ApiController(sender)
{
    [HttpGet("users")]
    [ProducesResponseType(typeof(IReadOnlyList<UserDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUsers()
    {
        return await SendAsync(new GetAllUsersQuery()).ToActionResultAsync();
    }

    [HttpPost("users/{userId:guid}/approve")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ApproveUser(Guid userId, [FromBody] ApproveUserRequest request)
    {
        return await SendAsync(new ApproveUserCommand(userId, request.FacilityId, request.Role)).ToActionResultAsync();
    }

    [HttpDelete("users/{userId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteUser(Guid userId)
    {
        return await SendAsync(new DeleteUserCommand(userId)).ToActionResultAsync();
    }

    [HttpGet("facilities")]
    [ProducesResponseType(typeof(IReadOnlyList<FacilityDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetFacilities()
    {
        return await SendAsync(new GetAllFacilitiesQuery()).ToActionResultAsync();
    }

    [HttpPost("facilities")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateFacility([FromBody] CreateFacilityCommand command)
    {
        return await SendAsync(command).ToActionResultAsync(id => CreatedAtAction(nameof(GetFacilities), new { id }, id));
    }

    [HttpDelete("facilities/{facilityId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteFacility(Guid facilityId)
    {
        return await SendAsync(new DeleteFacilityCommand(facilityId)).ToActionResultAsync();
    }
}
