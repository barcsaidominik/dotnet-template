using ErrorOr;
using Template.Application.Common.Dtos;

namespace Template.Application.Common.Interfaces;

public interface IAuthService
{
    Task<ErrorOr<Success>> RegisterAsync(string email, string password, CancellationToken ct = default);
    Task<ErrorOr<LoginResult>> LoginAsync(string email, string password, CancellationToken ct = default);
    Task<ErrorOr<Success>> SetPasswordAsync(string email, string token, string newPassword, CancellationToken ct = default);
    Task<ErrorOr<CreateUserResult>> CreateFacilityUserAsync(string email, Guid facilityId, string role, CancellationToken ct = default);
    Task<ErrorOr<Success>> ApproveUserAsync(Guid userId, Guid facilityId, string role, CancellationToken ct = default);
    Task<ErrorOr<Success>> DeleteUserAsync(Guid userId, CancellationToken ct = default);
    Task<ErrorOr<Updated>> UpdateUserAsync(Guid userId, string email, CancellationToken ct = default);
    Task<ErrorOr<Success>> UpdateUserRoleAsync(Guid userId, string newRole, CancellationToken ct = default);
    Task<ErrorOr<IReadOnlyList<UserDto>>> GetAllUsersAsync(CancellationToken ct = default);
    Task<ErrorOr<IReadOnlyList<UserDto>>> GetFacilityUsersAsync(Guid facilityId, CancellationToken ct = default);
    Task<ErrorOr<UserDto>> GetUserByIdAsync(Guid userId, CancellationToken ct = default);
    Task<ErrorOr<LoginResult>> RefreshAsync(string refreshToken, CancellationToken ct = default);
    Task<ErrorOr<Success>> LogoutAsync(string refreshToken, CancellationToken ct = default);
    Task<ErrorOr<Updated>> UpdatePreferredLanguageAsync(Guid userId, string language, CancellationToken ct = default);
    Task<string?> GetUserPreferredLanguageAsync(Guid userId, CancellationToken ct = default);
    Task<IReadOnlyDictionary<Guid, int>> GetUserCountsByFacilityAsync(CancellationToken ct = default);
    Task<string?> ForgotPasswordAsync(string email, CancellationToken ct = default);
}
