using Microsoft.EntityFrameworkCore;
using Template.Application.Common.Interfaces;
using Template.Domain.Common;

namespace Template.Infrastructure.Persistence;

public sealed class EntityStore<T>(AppDbContext context, IEnumerable<IQueryGuard<T>> guards) : IEntityStore<T> where T : Entity
{
    private readonly AppDbContext _context = context;
    private readonly IEnumerable<IQueryGuard<T>> _guards = guards;

    public IQueryable<T> GetQuery(bool asNoTracking = false, bool skipGuards = false)
    {
        var query = _context.Set<T>().AsQueryable();
        if (asNoTracking)
        {
            query = query.AsNoTracking();
        }

        if (!skipGuards && _guards is not null)
        {
            foreach (var guard in _guards)
            {
                query = guard.Apply(query);
            }
        }

        return query;
    }

    public async Task AddAsync(T entity, CancellationToken ct = default)
    {
        await _context.Set<T>().AddAsync(entity, ct);
    }

    public Task RemoveAsync(T entity, CancellationToken ct = default)
    {
        _context.Set<T>().Remove(entity);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken ct = default)
    {
        return _context.SaveChangesAsync(ct);
    }
}
