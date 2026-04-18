using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Template.Api.Extensions;
using Template.Application.Auth.Commands.Login;
using Template.Application.Auth.Commands.Register;
using Template.Application.Auth.Commands.SetPassword;
using Template.Application.Common.Dtos;

namespace Template.Api.Controllers;

[Route("api/[controller]")]
public sealed class AuthController(ISender sender) : ApiController(sender)
{
    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register([FromBody] RegisterCommand command)
    {
        return await SendAsync(command).ToActionResultAsync();
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LoginResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginCommand command)
    {
        return await SendAsync(command).ToActionResultAsync();
    }

    [HttpPost("set-password")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SetPassword([FromBody] SetPasswordCommand command)
    {
        return await SendAsync(command).ToActionResultAsync();
    }
}
