using Formarca.Domain.Common;

namespace Formarca.Application.Common.Interfaces;

public interface IGenericRepository
{
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
