using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Template.Api.Contracts;
using Template.Application.Users.Commands.UpdateUserLanguage;
using Template.Common.Extensions;

namespace Template.Api.Controllers;

[Authorize]
[Route("api/users")]
public sealed class UsersController(ISender sender)
    : Template.Common.Controllers.ApiController(sender)
{
    [HttpPut("me/language")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdateLanguage([FromBody] UpdateLanguageRequest request)
    {
        return await SendAsync(new UpdateUserLanguageCommand(request.Language))
            .ToActionResultAsync(static _ => new NoContentResult());
    }
}
