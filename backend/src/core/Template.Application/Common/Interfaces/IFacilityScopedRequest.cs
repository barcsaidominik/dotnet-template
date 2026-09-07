namespace Template.Application.Common.Interfaces;

/// <summary>
/// Marks a request whose <see cref="FacilityId"/> originates from untrusted input (route, query or body)
/// and therefore has to be validated against the caller's own facility.
/// Requests carrying this marker are checked by <c>FacilityScopeBehavior</c> before the handler runs.
/// Do not apply it to system-administration requests, where accessing an arbitrary facility is intentional.
/// </summary>
public interface IFacilityScopedRequest
{
    Guid FacilityId
    {
        get;
    }
}
