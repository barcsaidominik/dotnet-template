using Template.Domain.Common;

namespace Template.Application.Common.Interfaces;

public interface IQueryGuard<T> where T : Entity {
    IQueryable<T> Apply(IQueryable<T> query);
}
