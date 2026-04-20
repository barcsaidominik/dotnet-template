namespace Template.Application.Common.Interfaces;

public interface IFacilityProductUsageService
{
    Task<int> GetProductCountAsync(Guid facilityId, CancellationToken ct = default);
}
