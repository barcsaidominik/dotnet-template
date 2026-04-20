using ErrorOr;
using Template.Domain.Common;
using Template.Domain.Errors;

namespace Template.Domain.Entities;

public class Facility : Entity
{
    private Facility()
    {
    }

    public string Name { get; private set; } = default!;

    public static Facility Create(string name)
    {
        return new()
        {
            Name = name
        };
    }

    public ErrorOr<Updated> Update(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return FacilityErrors.InvalidName;
        }

        Name = name;

        return Result.Updated;
    }
}
