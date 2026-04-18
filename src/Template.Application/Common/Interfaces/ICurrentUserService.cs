namespace Template.Application.Common.Interfaces;

public interface ICurrentUserService
{
    Guid UserId { get; }
    Guid? FacilityId { get; }
    bool IsAuthenticated { get; }
}
