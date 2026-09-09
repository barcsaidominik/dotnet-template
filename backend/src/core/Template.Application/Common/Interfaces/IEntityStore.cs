using Template.Domain.Common;

namespace Template.Application.Common.Interfaces;

public interface IEntityStore<T> where T : Entity
{
    IQueryable<T> GetQuery(bool asNoTracking = false, bool skipGuards = false);
    Task AddAsync(T entity, CancellationToken ct = default);
    Task RemoveAsync(T entity, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
    Task<IEnumerable<T>> SearchAsync(string searchedColumn, string searchTerm, CancellationToken ct = default);
}
