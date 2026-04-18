namespace Template.Application.Common.Interfaces;

public interface IJwtTokenService {
    string GenerateToken(Guid userId, string email, Guid? facilityId, IList<string> roles);
}
