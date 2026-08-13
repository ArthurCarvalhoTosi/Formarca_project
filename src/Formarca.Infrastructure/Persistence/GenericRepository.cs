using EFCore.BulkExtensions;
using Formarca.Application.Common.Interfaces;
using Formarca.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace Formarca.Infrastructure.Persistence;

public sealed class GenericRepository(AppDbContext db) : IGenericRepository
{
    public async Task<T?> GetByIdAsync<T>(Guid id, CancellationToken cancellationToken = default)
        where T : Entity
        => await db.Set<T>().FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

    public async Task InsertAsync<T>(T entity, CancellationToken cancellationToken = default)
        where T : Entity
    {
        await db.Set<T>().AddAsync(entity, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task UpsertAsync<T>(T entity, CancellationToken cancellationToken = default)
        where T : Entity
    {
        var exists = await db.Set<T>().AsNoTracking().AnyAsync(e => e.Id == entity.Id, cancellationToken);
        if (exists)
        {
            entity.MarkUpdated();
            db.Set<T>().Update(entity);
        }
        else
        {
            await db.Set<T>().AddAsync(entity, cancellationToken);
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync<T>(T entity, CancellationToken cancellationToken = default)
        where T : Entity
    {
        entity.SoftDelete();
        db.Set<T>().Update(entity);
        await db.SaveChangesAsync(cancellationToken);
    }

    public Task BulkInsertAsync<T>(IList<T> entities, CancellationToken cancellationToken = default)
        where T : Entity
        => BulkAsync(entities, (list, token) => db.BulkInsertAsync(list, cancellationToken: token), cancellationToken);

    public Task BulkInsertOrUpdateAsync<T>(IList<T> entities, CancellationToken cancellationToken = default)
        where T : Entity
        => BulkUpsertAsync(entities, cancellationToken);

    public Task BulkUpsertAsync<T>(IList<T> entities, CancellationToken cancellationToken = default)
        where T : Entity
    {
        foreach (var entity in entities)
        {
            entity.MarkUpdated();
        }

        return BulkAsync(
            entities,
            (list, token) => db.BulkInsertOrUpdateAsync(list, cancellationToken: token),
            cancellationToken);
    }

    public Task BulkUpdateAsync<T>(IList<T> entities, CancellationToken cancellationToken = default)
        where T : Entity
    {
        foreach (var entity in entities)
        {
            entity.MarkUpdated();
        }

        return BulkAsync(entities, (list, token) => db.BulkUpdateAsync(list, cancellationToken: token), cancellationToken);
    }

    public Task BulkDeleteAsync<T>(IList<T> entities, CancellationToken cancellationToken = default)
        where T : Entity
        => BulkAsync(entities, (list, token) => db.BulkDeleteAsync(list, cancellationToken: token), cancellationToken);

    public Task<int> CommitAsync(CancellationToken cancellationToken = default)
        => db.SaveChangesAsync(cancellationToken);

    private static async Task BulkAsync<T>(
        IList<T> entities,
        Func<IList<T>, CancellationToken, Task> action,
        CancellationToken cancellationToken)
        where T : Entity
    {
        if (entities.Count == 0)
        {
            return;
        }

        await action(entities, cancellationToken);
    }
}
