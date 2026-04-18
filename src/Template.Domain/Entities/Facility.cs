using Template.Domain.Common;

namespace Template.Domain.Entities;

public class Facility : Entity
{
    private Facility() { }

    public string Name { get; private set; } = default!;

    public static Facility Create(string name) => new() { Name = name };
}
