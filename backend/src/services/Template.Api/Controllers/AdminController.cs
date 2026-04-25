using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Template.Api.Contracts;
using Template.Application.Admin.Commands.ApproveUser;
using Template.Application.Admin.Commands.CreateFacility;
using Template.Application.Admin.Commands.DeleteFacility;
using Template.Application.Admin.Commands.DeleteUser;
using Template.Application.Admin.Commands.UpdateFacility;
using Template.Application.Admin.Commands.UpdateUser;
using Template.Application.Admin.Queries.ExportFacilitiesToExcel;
using Template.Application.Admin.Queries.ExportUsersToExcel;
using Template.Application.Admin.Queries.GetAllFacilities;
using Template.Application.Admin.Queries.GetAllUsers;
using Template.Application.Admin.Queries.GetAuditLogs;
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
    public async Task<IActionResult> ExportUsers(
        [FromQuery] string? search = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] bool sortDescending = false)
    {
        return await SendAsync(new ExportUsersToExcelQuery(search, sortBy, sortDescending))
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

    [HttpPut("users/{userId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateUser(Guid userId, [FromBody] UpdateUserRequest request)
    {
        return await SendAsync(new UpdateUserCommand(userId, request.Email))
            .ToActionResultAsync(_ => NoContent());
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
    public async Task<IActionResult> ExportFacilities(
        [FromQuery] string? search = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] bool sortDescending = false)
    {
        return await SendAsync(new ExportFacilitiesToExcelQuery(search, sortBy, sortDescending))
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
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateFacility(Guid facilityId, [FromBody] UpdateFacilityRequest request)
    {
        return await SendAsync(new UpdateFacilityCommand(facilityId, request.Name, request.RowVersion))
            .ToActionResultAsync(_ => NoContent());
    }

    [HttpPost("jobs/demo-long-running")]
    [ProducesResponseType(typeof(QueuedBackgroundJobResult), StatusCodes.Status202Accepted)]
    public async Task<IActionResult> ScheduleDemoLongRunningJob(CancellationToken cancellationToken)
    {
        var result = await _backgroundJobScheduler.ScheduleDemoLongRunningOperationAsync(cancellationToken);
        return Accepted(result);
    }

    [HttpGet("audit")]
    [ProducesResponseType(typeof(PagedResult<AuditEntryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAuditLog(
        [FromQuery] string? entityType = null,
        [FromQuery] string? entityId = null,
        [FromQuery] string? action = null,
        [FromQuery] Guid? userId = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] string? sortBy = null,
        [FromQuery] bool sortDescending = true)
    {
        return await SendAsync(new GetAuditLogsQuery(
            entityType, entityId, action, userId, from, to, page, pageSize, sortBy, sortDescending))
            .ToActionResultAsync();
    }

    [HttpGet("reports/download")]
    public async Task<IActionResult> DownloadReport(
        [FromQuery] string reportName,
        [FromServices] IReportService reportService,
        CancellationToken ct)
    {
        var content = await reportService.GetReportAsync(reportName, ct);
        return File(content, "application/octet-stream", reportName);
    }
}
