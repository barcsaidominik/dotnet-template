using Microsoft.AspNetCore.Identity;
using Template.Domain.Entities;

namespace Template.Infrastructure.Identity;

public sealed class AppUser : IdentityUser<Guid>
{
    public Guid? FacilityId
    {
        get; set;
    }
    public bool IsApproved
    {
        get; set;
    }
    public bool RequiresPasswordChange
    {
        get; set;
    }
    public Facility? Facility
    {
        get; set;
    }
    public string? RefreshToken
    {
        get; set;
    }
    public DateTime? RefreshTokenExpiry
    {
        get; set;
    }
}
