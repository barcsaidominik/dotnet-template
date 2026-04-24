using System.Security.Cryptography;
using ErrorOr;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Template.Application.Common.Dtos;
using Template.Application.Common.Interfaces;
using Template.Domain.Errors;
using Template.Infrastructure.Persistence;

namespace Template.Infrastructure.Identity;

public sealed class AuthService(
    UserManager<AppUser> userManager,
    RoleManager<AppRole> roleManager,
    IJwtTokenService jwtTokenService,
    AppDbContext dbContext) : IAuthService
{
    private readonly UserManager<AppUser> _userManager = userManager;
    private readonly RoleManager<AppRole> _roleManager = roleManager;
    private readonly IJwtTokenService _jwtTokenService = jwtTokenService;
    private readonly AppDbContext _dbContext = dbContext;

    public async Task<ErrorOr<Success>> RegisterAsync(string email, string password, CancellationToken ct = default)
    {
        var existing = await _userManager.FindByEmailAsync(email);
        if (existing is not null)
        {
            return FacilityErrors.UserAlreadyExists;
        }

        var user = new AppUser
        {
            UserName = email,
            Email = email,
            IsApproved = false,
            RequiresPasswordChange = false
        };

        var result = await _userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            return result.Errors
                .Select(e => Error.Validation(e.Code, e.Description))
                .ToList();
        }

        return Result.Success;
    }

    public async Task<ErrorOr<LoginResult>> LoginAsync(string email, string password, CancellationToken ct = default)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user is null)
        {
            return AuthErrors.InvalidCredentials;
        }

        if (await _userManager.IsLockedOutAsync(user))
        {
            return AuthErrors.AccountLocked;
        }

        var valid = await _userManager.CheckPasswordAsync(user, password);
        if (!valid)
        {
            await _userManager.AccessFailedAsync(user);
            return AuthErrors.InvalidCredentials;
        }

        await _userManager.ResetAccessFailedCountAsync(user);

        if (!user.IsApproved)
        {
            return AuthErrors.NotApproved;
        }

        if (user.RequiresPasswordChange)
        {
            return AuthErrors.PasswordChangeRequired;
        }

        var roles = await _userManager.GetRolesAsync(user);

        var refreshToken = GenerateRefreshToken();
        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(7);
        await _userManager.UpdateAsync(user);

        var accessToken = _jwtTokenService.GenerateToken(user.Id, user.Email!, user.FacilityId, roles);
        return new LoginResult(accessToken, DateTime.UtcNow.AddMinutes(15), roles.FirstOrDefault() ?? string.Empty, refreshToken, user.PreferredLanguage);
    }

    public async Task<ErrorOr<Success>> SetPasswordAsync(string email, string token, string newPassword, CancellationToken ct = default)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user is null)
        {
            return AuthErrors.InvalidCredentials;
        }

        var result = await _userManager.ResetPasswordAsync(user, token, newPassword);
        if (!result.Succeeded)
        {
            return result.Errors.Select(e => Error.Validation(e.Code, e.Description)).ToList();
        }

        user.RequiresPasswordChange = false;
        await _userManager.UpdateAsync(user);
        return Result.Success;
    }

    public async Task<ErrorOr<Success>> ApproveUserAsync(Guid userId, Guid facilityId, string role, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return AuthErrors.UserNotFound;
        }

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
        {
            return AuthErrors.UserNotFound;
        }

        await _userManager.DeleteAsync(user);
        return Result.Success;
    }

    public async Task<ErrorOr<Updated>> UpdateUserAsync(Guid userId, string email, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return AuthErrors.UserNotFound;
        }

        var existingWithEmail = await _userManager.FindByEmailAsync(email);
        if (existingWithEmail is not null && existingWithEmail.Id != userId)
        {
            return FacilityErrors.UserAlreadyExists;
        }

        user.Email = email;
        user.UserName = email;
        user.NormalizedEmail = email.ToUpperInvariant();
        user.NormalizedUserName = email.ToUpperInvariant();

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            return result.Errors.Select(e => Error.Validation(e.Code, e.Description)).ToList();
        }

        return Result.Updated;
    }

    public async Task<ErrorOr<Success>> UpdateUserRoleAsync(Guid userId, string newRole, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return AuthErrors.UserNotFound;
        }

        var currentRoles = await _userManager.GetRolesAsync(user);
        await _userManager.RemoveFromRolesAsync(user, currentRoles);
        await _userManager.AddToRoleAsync(user, newRole);
        return Result.Success;
    }

    public async Task<ErrorOr<CreateUserResult>> CreateFacilityUserAsync(string email, Guid facilityId, string role, CancellationToken ct = default)
    {
        var existing = await _userManager.FindByEmailAsync(email);
        if (existing is not null)
        {
            return FacilityErrors.UserAlreadyExists;
        }

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
        {
            return result.Errors.Select(e => Error.Validation(e.Code, e.Description)).ToList();
        }

        await _userManager.AddToRoleAsync(user, role);

        var setupToken = await _userManager.GeneratePasswordResetTokenAsync(user);
        return new CreateUserResult(user.Id, setupToken);
    }

    public async Task<ErrorOr<IReadOnlyList<UserDto>>> GetAllUsersAsync(CancellationToken ct = default)
    {
        var users = await _userManager.Users.Take(10_000).ToListAsync(ct);
        var rolesByUserId = await GetRolesByUserIdsAsync(users.Select(u => u.Id).ToList(), ct);

        var result = users.Select(user =>
        {
            rolesByUserId.TryGetValue(user.Id, out var roles);
            return new UserDto(user.Id, user.Email!, user.FacilityId, user.IsApproved, roles?.FirstOrDefault());
        }).ToList();

        return result.AsReadOnly();
    }

    public async Task<ErrorOr<IReadOnlyList<UserDto>>> GetFacilityUsersAsync(Guid facilityId, CancellationToken ct = default)
    {
        var users = await _userManager.Users.Where(u => u.FacilityId == facilityId).ToListAsync(ct);
        var rolesByUserId = await GetRolesByUserIdsAsync(users.Select(u => u.Id).ToList(), ct);

        var result = users.Select(user =>
        {
            rolesByUserId.TryGetValue(user.Id, out var roles);
            return new UserDto(user.Id, user.Email!, user.FacilityId, user.IsApproved, roles?.FirstOrDefault());
        }).ToList();

        return result.AsReadOnly();
    }

    public async Task<ErrorOr<UserDto>> GetUserByIdAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return AuthErrors.UserNotFound;
        }

        var roles = await _userManager.GetRolesAsync(user);
        return new UserDto(user.Id, user.Email!, user.FacilityId, user.IsApproved, roles.FirstOrDefault());
    }

    public async Task<ErrorOr<LoginResult>> RefreshAsync(string refreshToken, CancellationToken ct = default)
    {
        var hashedToken = HashToken(refreshToken);
        var user = await _userManager.Users.SingleOrDefaultAsync(u => u.RefreshToken == hashedToken, ct);
        if (user is null || user.RefreshTokenExpiry <= DateTime.UtcNow)
        {
            return AuthErrors.InvalidCredentials;
        }

        var roles = await _userManager.GetRolesAsync(user);

        var newRefreshToken = GenerateRefreshToken();
        user.RefreshToken = HashToken(newRefreshToken);
        user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(7);
        await _userManager.UpdateAsync(user);

        var accessToken = _jwtTokenService.GenerateToken(user.Id, user.Email!, user.FacilityId, roles);
        return new LoginResult(accessToken, DateTime.UtcNow.AddMinutes(15), roles.FirstOrDefault() ?? string.Empty, newRefreshToken, user.PreferredLanguage);
    }

    public async Task<ErrorOr<Success>> LogoutAsync(string refreshToken, CancellationToken ct = default)
    {
        var hashedToken = HashToken(refreshToken);
        var user = await _userManager.Users.SingleOrDefaultAsync(u => u.RefreshToken == hashedToken, ct);
        if (user is null)
        {
            return Result.Success;
        }

        user.RefreshToken = null;
        user.RefreshTokenExpiry = null;
        await _userManager.UpdateAsync(user);
        return Result.Success;
    }

    public async Task<ErrorOr<Updated>> UpdatePreferredLanguageAsync(Guid userId, string language, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return AuthErrors.UserNotFound;
        }

        user.PreferredLanguage = language;
        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            return result.Errors.Select(e => Error.Validation(e.Code, e.Description)).ToList();
        }

        return Result.Updated;
    }

    public async Task<string?> GetUserPreferredLanguageAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        return user?.PreferredLanguage;
    }

    public async Task<IReadOnlyDictionary<Guid, int>> GetUserCountsByFacilityAsync(CancellationToken ct = default)
    {
        var counts = await _userManager.Users
            .Where(u => u.FacilityId != null)
            .GroupBy(u => u.FacilityId!.Value)
            .Select(g => new { FacilityId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.FacilityId, x => x.Count, ct);

        return counts;
    }

    private static string GenerateRefreshToken()
    {
        var bytes = new byte[64];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes);
    }

    private static string HashToken(string token)
    {
        var bytes = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes);
    }

    public async Task<string?> ForgotPasswordAsync(string email, CancellationToken ct = default)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user is null)
        {
            return null;
        }

        return await _userManager.GeneratePasswordResetTokenAsync(user);
    }

    private async Task<Dictionary<Guid, IList<string>>> GetRolesByUserIdsAsync(List<Guid> userIds, CancellationToken ct)
    {
        var userRoles = await _dbContext.UserRoles
            .Where(ur => userIds.Contains(ur.UserId))
            .ToListAsync(ct);

        var roleIds = userRoles.Select(ur => ur.RoleId).Distinct().ToList();
        var roles = await _dbContext.Roles
            .Where(r => roleIds.Contains(r.Id))
            .ToDictionaryAsync(r => r.Id, r => r.Name!, ct);

        return userRoles
            .GroupBy(ur => ur.UserId)
            .ToDictionary(
                g => g.Key,
                g => (IList<string>)g.Select(ur => roles.TryGetValue(ur.RoleId, out var name) ? name : string.Empty)
                    .Where(n => !string.IsNullOrEmpty(n))
                    .ToList());
    }
}
