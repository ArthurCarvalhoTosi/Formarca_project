using System.Linq.Expressions;
using Formarca.Domain.Common;

namespace Formarca.Application.Common.Interfaces;

public interface IGenericRepository
{
    IQueryable<T> Query<T>(bool asNoTracking = true) where T : Entity;

    Task<T?> FirstOrDefaultAsync<T>(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
        where T : Entity;

    Task<bool> AnyAsync<T>(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
        where T : Entity;

    void Add<T>(T entity) where T : Entity;

    Task<T?> GetByIdAsync<T>(Guid id, CancellationToken cancellationToken = default) where T : Entity;

    Task InsertAsync<T>(T entity, CancellationToken cancellationToken = default) where T : Entity;

    Task UpsertAsync<T>(T entity, CancellationToken cancellationToken = default) where T : Entity;

    Task DeleteAsync<T>(T entity, CancellationToken cancellationToken = default) where T : Entity;

    Task BulkInsertAsync<T>(IList<T> entities, CancellationToken cancellationToken = default) where T : Entity;

    Task BulkInsertOrUpdateAsync<T>(IList<T> entities, CancellationToken cancellationToken = default) where T : Entity;

    Task BulkUpsertAsync<T>(IList<T> entities, CancellationToken cancellationToken = default) where T : Entity;

    Task BulkUpdateAsync<T>(IList<T> entities, CancellationToken cancellationToken = default) where T : Entity;

    Task BulkDeleteAsync<T>(IList<T> entities, CancellationToken cancellationToken = default) where T : Entity;

    Task<int> CommitAsync(CancellationToken cancellationToken = default);
}
