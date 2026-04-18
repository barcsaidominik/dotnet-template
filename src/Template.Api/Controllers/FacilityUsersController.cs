using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Template.Api.Contracts;
using Template.Api.Extensions;
using Template.Application.Common.Dtos;
using Template.Application.Facilities.Commands.CreateFacilityUser;
using Template.Application.Facilities.Commands.RemoveFacilityUser;
using Template.Application.Facilities.Commands.UpdateFacilityUserRole;
using Template.Application.Facilities.Queries.GetFacilityUsers;
using Template.Domain.Constants;

namespace Template.Api.Controllers;

[Route("api/facilities/{facilityId:guid}/users")]
[Authorize(Roles = Roles.FacilityAdmin + "," + Roles.SystemAdmin)]
public sealed class FacilityUsersController(ISender sender) : ApiController(sender)
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<UserDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUsers(Guid facilityId)
    {
        return await SendAsync(new GetFacilityUsersQuery(facilityId)).ToActionResultAsync();
    }

    [HttpPost]
    [ProducesResponseType(typeof(CreateUserResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateUser(Guid facilityId, [FromBody] CreateFacilityUserRequest request)
    {
        return await SendAsync(new CreateFacilityUserCommand(facilityId, request.Email, request.Role)).ToActionResultAsync();
    }

    [HttpDelete("{userId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> RemoveUser(Guid facilityId, Guid userId)
    {
        return await SendAsync(new RemoveFacilityUserCommand(facilityId, userId)).ToActionResultAsync();
    }

    [HttpPut("{userId:guid}/role")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateRole(Guid facilityId, Guid userId, [FromBody] UpdateRoleRequest request)
    {
        return await SendAsync(new UpdateFacilityUserRoleCommand(facilityId, userId, request.Role)).ToActionResultAsync();
    }
}
