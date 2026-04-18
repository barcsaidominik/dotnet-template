using ErrorOr;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Template.Application.Common.Dtos;
using Template.Application.Common.Interfaces;
using Template.Domain.Errors;

namespace Template.Infrastructure.Identity;

public sealed class AuthService : IAuthService
{
    private readonly UserManager<AppUser> _userManager;
    private readonly RoleManager<AppRole> _roleManager;
    private readonly IJwtTokenService _jwtTokenService;

    public AuthService(
        UserManager<AppUser> userManager,
        RoleManager<AppRole> roleManager,
        IJwtTokenService jwtTokenService)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _jwtTokenService = jwtTokenService;
    }

    public async Task<ErrorOr<Success>> RegisterAsync(string email, string password, CancellationToken ct = default)
    {
        var existing = await _userManager.FindByEmailAsync(email);
        if (existing is not null)
            return FacilityErrors.UserAlreadyExists;

        var user = new AppUser
        {
            UserName = email,
            Email = email,
            IsApproved = false,
            RequiresPasswordChange = false
        };

        var result = await _userManager.CreateAsync(user, password);
        if (!result.Succeeded)
            return result.Errors
                .Select(e => Error.Validation(e.Code, e.Description))
                .ToList();

        return Result.Success;
    }

    public async Task<ErrorOr<LoginResult>> LoginAsync(string email, string password, CancellationToken ct = default)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user is null)
            return AuthErrors.InvalidCredentials;

        var valid = await _userManager.CheckPasswordAsync(user, password);
        if (!valid)
            return AuthErrors.InvalidCredentials;

        if (!user.IsApproved)
            return AuthErrors.NotApproved;

        if (user.RequiresPasswordChange)
            return AuthErrors.PasswordChangeRequired;

        var roles = await _userManager.GetRolesAsync(user);
        var token = _jwtTokenService.GenerateToken(user.Id, user.Email!, user.FacilityId, roles);
        var expiryMinutes = 60;

        return new LoginResult(token, DateTime.UtcNow.AddMinutes(expiryMinutes), roles.FirstOrDefault() ?? string.Empty);
    }

    public async Task<ErrorOr<Success>> SetPasswordAsync(string email, string token, string newPassword, CancellationToken ct = default)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user is null)
            return AuthErrors.InvalidCredentials;

        var result = await _userManager.ResetPasswordAsync(user, token, newPassword);
        if (!result.Succeeded)
            return result.Errors.Select(e => Error.Validation(e.Code, e.Description)).ToList();

        user.RequiresPasswordChange = false;
        await _userManager.UpdateAsync(user);
        return Result.Success;
    }

    public async Task<ErrorOr<Success>> ApproveUserAsync(Guid userId, Guid facilityId, string role, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null)
            return AuthErrors.UserNotFound;

        user.IsApproved = true;
        user.FacilityId = facilityId;
        await _userManager.UpdateAsync(user);

        var currentRoles = await _userManager.GetRolesAsync(user);
        await _userManager.RemoveFromRolesAsync(user, currentRoles);
        await _userManager.AddToRoleAsync(user, role);

        return Result.Success;
    }

    public async Task<ErrorOr<Success>> DeleteUserAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null)
            return AuthErrors.UserNotFound;

        await _userManager.DeleteAsync(user);
        return Result.Success;
    }

    public async Task<ErrorOr<Success>> UpdateUserRoleAsync(Guid userId, string newRole, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null)
            return AuthErrors.UserNotFound;

        var currentRoles = await _userManager.GetRolesAsync(user);
        await _userManager.RemoveFromRolesAsync(user, currentRoles);
        await _userManager.AddToRoleAsync(user, newRole);
        return Result.Success;
    }

    public async Task<ErrorOr<CreateUserResult>> CreateFacilityUserAsync(string email, Guid facilityId, string role, CancellationToken ct = default)
    {
        var existing = await _userManager.FindByEmailAsync(email);
        if (existing is not null)
            return FacilityErrors.UserAlreadyExists;

        var user = new AppUser
        {
            UserName = email,
            Email = email,
            FacilityId = facilityId,
            IsApproved = true,
            RequiresPasswordChange = true
        };

        var result = await _userManager.CreateAsync(user);
        if (!result.Succeeded)
            return result.Errors.Select(e => Error.Validation(e.Code, e.Description)).ToList();

        await _userManager.AddToRoleAsync(user, role);

        var setupToken = await _userManager.GeneratePasswordResetTokenAsync(user);
        return new CreateUserResult(user.Id, setupToken);
    }

    public async Task<ErrorOr<IReadOnlyList<UserDto>>> GetAllUsersAsync(CancellationToken ct = default)
    {
        var users = await _userManager.Users.ToListAsync(ct);
        var result = new List<UserDto>();
        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            result.Add(new UserDto(user.Id, user.Email!, user.FacilityId, user.IsApproved, roles.FirstOrDefault()));
        }
        return result.AsReadOnly();
    }

    public async Task<ErrorOr<IReadOnlyList<UserDto>>> GetFacilityUsersAsync(Guid facilityId, CancellationToken ct = default)
    {
        var users = await _userManager.Users.Where(u => u.FacilityId == facilityId).ToListAsync(ct);
        var result = new List<UserDto>();
        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            result.Add(new UserDto(user.Id, user.Email!, user.FacilityId, user.IsApproved, roles.FirstOrDefault()));
        }
        return result.AsReadOnly();
    }

    public async Task<ErrorOr<UserDto>> GetUserByIdAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null)
            return AuthErrors.UserNotFound;

        var roles = await _userManager.GetRolesAsync(user);
        return new UserDto(user.Id, user.Email!, user.FacilityId, user.IsApproved, roles.FirstOrDefault());
    }
}
