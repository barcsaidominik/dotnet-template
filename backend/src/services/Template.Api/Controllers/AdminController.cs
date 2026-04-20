using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Template.Api.Contracts;
using Template.Application.Admin.Commands.ApproveUser;
using Template.Application.Admin.Commands.CreateFacility;
using Template.Application.Admin.Commands.DeleteFacility;
using Template.Application.Admin.Commands.DeleteUser;
using Template.Application.Admin.Commands.UpdateFacility;
using Template.Application.Admin.Queries.ExportFacilitiesToExcel;
using Template.Application.Admin.Queries.ExportUsersToExcel;
using Template.Application.Admin.Queries.GetAllFacilities;
using Template.Application.Admin.Queries.GetAllUsers;
using Template.Application.Common.Dtos;
using Template.Application.Common.Interfaces;
using Template.Common.Extensions;
using Template.Domain.Constants;

namespace Template.Api.Controllers;

[Route("api/[controller]")]
[Authorize(Roles = Roles.SYSTEM_ADMIN)]
public sealed class AdminController(ISender sender, IBackgroundJobScheduler backgroundJobScheduler)
    : Template.Common.Controllers.ApiController(sender)
{
    private readonly IBackgroundJobScheduler _backgroundJobScheduler = backgroundJobScheduler;

    [HttpGet("users")]
    [ProducesResponseType(typeof(IReadOnlyList<UserDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUsers(
        [FromQuery] string? search = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] bool sortDescending = false)
    {
        return await SendAsync(new GetAllUsersQuery(search, sortBy, sortDescending)).ToActionResultAsync();
    }

    [HttpGet("users/export")]
    [Produces("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
    public async Task<IActionResult> ExportUsers()
    {
        return await SendAsync(new ExportUsersToExcelQuery())
            .ToActionResultAsync(file => File(file.Content, file.ContentType, file.FileName));
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
    [ProducesResponseType(typeof(IReadOnlyList<FacilityWithCountDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetFacilities(
        [FromQuery] string? search = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] bool sortDescending = false)
    {
        return await SendAsync(new GetAllFacilitiesQuery(search, sortBy, sortDescending)).ToActionResultAsync();
    }

    [HttpGet("facilities/export")]
    [Produces("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
    public async Task<IActionResult> ExportFacilities()
    {
        return await SendAsync(new ExportFacilitiesToExcelQuery())
            .ToActionResultAsync(file => File(file.Content, file.ContentType, file.FileName));
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

    [HttpPut("facilities/{facilityId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateFacility(Guid facilityId, [FromBody] UpdateFacilityRequest request)
    {
        return await SendAsync(new UpdateFacilityCommand(facilityId, request.Name))
            .ToActionResultAsync(_ => NoContent());
    }

    [HttpPost("jobs/demo-long-running")]
    [ProducesResponseType(typeof(QueuedBackgroundJobResult), StatusCodes.Status202Accepted)]
    public async Task<IActionResult> ScheduleDemoLongRunningJob(CancellationToken cancellationToken)
    {
        var result = await _backgroundJobScheduler.ScheduleDemoLongRunningOperationAsync(cancellationToken);
        return Accepted(result);
    }
}
