using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Template.Application.Auth.Commands.Login;
using Template.Application.Auth.Commands.Logout;
using Template.Application.Auth.Commands.Refresh;
using Template.Application.Auth.Commands.Register;
using Template.Application.Auth.Commands.SetPassword;
using Template.Application.Common.Dtos;
using Template.Common.Extensions;

namespace Template.Api.Controllers;

[Route("api/[controller]")]
public sealed class AuthController(ISender sender, IWebHostEnvironment environment)
    : Template.Common.Controllers.ApiController(sender)
{
    private const string REFRESH_TOKEN_COOKIE = "refreshToken";
    private readonly IWebHostEnvironment _environment = environment;

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
    [ProducesResponseType(typeof(TokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginCommand command)
    {
        var result = await SendAsync(command);
        if (result.IsError)
        {
            return result.ToActionResult();
        }

        SetRefreshTokenCookie(result.Value.RefreshToken);
        return Ok(new TokenResponse(result.Value.Token, result.Value.ExpiresAt, result.Value.Role, result.Value.PreferredLanguage));
    }

    [HttpPost("set-password")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SetPassword([FromBody] SetPasswordCommand command)
    {
        return await SendAsync(command).ToActionResultAsync();
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(TokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh()
    {
        var refreshToken = Request.Cookies[REFRESH_TOKEN_COOKIE];
        if (string.IsNullOrEmpty(refreshToken))
        {
            return Unauthorized();
        }

        var result = await SendAsync(new RefreshCommand(refreshToken));
        if (result.IsError)
        {
            return result.ToActionResult();
        }

        SetRefreshTokenCookie(result.Value.RefreshToken);
        return Ok(new TokenResponse(result.Value.Token, result.Value.ExpiresAt, result.Value.Role, result.Value.PreferredLanguage));
    }

    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Logout()
    {
        var refreshToken = Request.Cookies[REFRESH_TOKEN_COOKIE];
        if (!string.IsNullOrEmpty(refreshToken))
        {
            await SendAsync(new LogoutCommand(refreshToken));
        }

        Response.Cookies.Delete(REFRESH_TOKEN_COOKIE, new CookieOptions { Path = "/" });
        return Ok();
    }

    private void SetRefreshTokenCookie(string refreshToken)
    {
        var options = new CookieOptions
        {
            HttpOnly = true,
            SameSite = SameSiteMode.Strict,
            Secure = !_environment.IsDevelopment(),
            Expires = DateTimeOffset.UtcNow.AddDays(7),
            Path = "/"
        };
        Response.Cookies.Append(REFRESH_TOKEN_COOKIE, refreshToken, options);
    }
}
