using System.Linq.Expressions;
using Formarca.Application.Common.Exceptions;
using Formarca.Application.Common.Interfaces;
using Formarca.Application.Common.Models;
using Formarca.Application.Tenancy.Commands;
using Formarca.Application.Tenancy.Queries;
using Formarca.Domain.Common;
using Formarca.Domain.IdentityAccess;
using Formarca.Domain.Tenancy;
using Microsoft.EntityFrameworkCore.Query;

namespace Formarca.Application.Tests;

public class RegistrationHandlerTests
{
    [Fact]
    public async Task Submit_Creates_Pending_Tenant_Without_User()
    {
        var repository = new InMemoryRepository();
        var identity = new FakeIdentityService();
        var handler = new SubmitRegistrationCommandHandler(repository, identity, new FakeTenantContext());

        var result = await handler.Handle(ValidSubmit(), CancellationToken.None);

        Assert.Equal(TenantStatus.Pending.ToString(), result.Status);
        Assert.Empty(identity.CreatedUsers);
        Assert.False(repository.Query<UserTenantMembership>().Any());
        var tenant = repository.Query<Tenant>().Single();
        Assert.Equal(TenantStatus.Pending, tenant.Status);
        Assert.Equal("maria@acme.com", tenant.Email);
    }

    [Fact]
    public async Task Approve_Creates_User_And_Activates_Tenant()
    {
        var repository = new InMemoryRepository();
        repository.Add(Permission.Create(PermissionCodes.RegistrationsRead, "Read registration"));
        repository.Add(Permission.Create(PermissionCodes.RegistrationsManage, "Manage registration"));
        repository.Add(Permission.Create(PermissionCodes.ProcessesRead, "Read processes"));
        repository.Add(Permission.Create(PermissionCodes.UsersRead, "Read users"));
        repository.Add(Permission.Create(PermissionCodes.UsersManage, "Manage users"));
        repository.Add(Permission.Create(PermissionCodes.GroupsRead, "Read groups"));
        repository.Add(Permission.Create(PermissionCodes.GroupsManage, "Manage groups"));

        var tenant = Tenant.Register(
            "Acme Ltd",
            "acme-ltd",
            "maria@acme.com",
            "11987654321",
            "39053344705",
            Address.Create("01310100", "Av Paulista", "100", null, "Bela Vista", "São Paulo", "SP"),
            "Acme",
            "Widgets");
        repository.Add(tenant);

        var identity = new FakeIdentityService();
        var handler = new ApproveTenantCommandHandler(repository, identity, new FakeTenantContext());

        var result = await handler.Handle(new ApproveTenantCommand(tenant.Id, "Password1"), CancellationToken.None);

        Assert.Equal(TenantStatus.Active.ToString(), result.Status);
        Assert.Single(identity.CreatedUsers);
        Assert.Equal("maria@acme.com", identity.CreatedUsers[0].Email);
        Assert.Equal("Password1", identity.CreatedUsers[0].Password);
        Assert.True(repository.Query<UserTenantMembership>().Any(m => m.TenantId == tenant.Id && m.UserId == identity.NextUserId));
        Assert.True(repository.Query<UserGroupMembership>().Any(m => m.TenantId == tenant.Id && m.GroupId != Guid.Empty));
        Assert.Equal(TenantStatus.Active, repository.Query<Tenant>().Single().Status);
        Assert.Contains(repository.Query<Group>(), g => g.Name == DefaultGroupNames.TenantAdmins);
    }

    [Fact]
    public async Task GetByTenantId_Allows_Own_Tenant()
    {
        var (repository, own, _) = SeedTwoActiveTenants();
        var handler = new GetRegistrationByTenantIdQueryHandler(
            OwnTenantUser(own.Id), repository, new FakeTenantContext());

        var result = await handler.Handle(new GetRegistrationByTenantIdQuery(own.Id), CancellationToken.None);

        Assert.Equal(own.Id, result.Id);
        Assert.Equal("maria@acme.com", result.Email);
    }

    [Fact]
    public async Task GetByTenantId_Forbids_Other_Tenant()
    {
        var (repository, own, other) = SeedTwoActiveTenants();
        var handler = new GetRegistrationByTenantIdQueryHandler(
            OwnTenantUser(own.Id), repository, new FakeTenantContext());

        var ex = await Assert.ThrowsAsync<ForbiddenException>(
            () => handler.Handle(new GetRegistrationByTenantIdQuery(other.Id), CancellationToken.None));

        Assert.Equal("User is not a member of the requested tenant.", ex.Message);
    }

    [Fact]
    public async Task GetByTenantId_Allows_PlatformAdmin_Other_Tenant()
    {
        var (repository, _, other) = SeedTwoActiveTenants();
        var handler = new GetRegistrationByTenantIdQueryHandler(
            PlatformAdminUser(), repository, new FakeTenantContext());

        var result = await handler.Handle(new GetRegistrationByTenantIdQuery(other.Id), CancellationToken.None);

        Assert.Equal(other.Id, result.Id);
        Assert.Equal("other@acme.com", result.Email);
    }

    [Fact]
    public async Task UpdateByTenantId_Allows_Own_Tenant()
    {
        var (repository, own, _) = SeedTwoActiveTenants();
        var handler = new UpdateRegistrationByTenantIdCommandHandler(
            OwnTenantUser(own.Id), repository, new FakeIdentityService(), new FakeTenantContext());

        var result = await handler.Handle(ValidUpdateByTenantId(own.Id, "Updated Ltd", "updated@acme.com"), CancellationToken.None);

        Assert.Equal("Updated Ltd", result.Name);
        Assert.Equal("updated@acme.com", result.Email);
        Assert.Equal("Updated Ltd", repository.Query<Tenant>().Single(t => t.Id == own.Id).Name);
    }

    [Fact]
    public async Task UpdateByTenantId_Forbids_Other_Tenant()
    {
        var (repository, own, other) = SeedTwoActiveTenants();
        var handler = new UpdateRegistrationByTenantIdCommandHandler(
            OwnTenantUser(own.Id), repository, new FakeIdentityService(), new FakeTenantContext());

        var ex = await Assert.ThrowsAsync<ForbiddenException>(
            () => handler.Handle(ValidUpdateByTenantId(other.Id, "Hacked", "hacked@acme.com"), CancellationToken.None));

        Assert.Equal("User is not a member of the requested tenant.", ex.Message);
        Assert.Equal("Other Ltd", repository.Query<Tenant>().Single(t => t.Id == other.Id).Name);
    }

    [Fact]
    public async Task UpdateByTenantId_Allows_PlatformAdmin_Other_Tenant()
    {
        var (repository, _, other) = SeedTwoActiveTenants();
        var handler = new UpdateRegistrationByTenantIdCommandHandler(
            PlatformAdminUser(), repository, new FakeIdentityService(), new FakeTenantContext());

        var result = await handler.Handle(
            ValidUpdateByTenantId(other.Id, "Admin Edit", "admin-edit@acme.com", "52998224725"),
            CancellationToken.None);

        Assert.Equal("Admin Edit", result.Name);
        Assert.Equal("admin-edit@acme.com", result.Email);
    }

    private static (IGenericRepository Repository, Tenant Own, Tenant Other) SeedTwoActiveTenants()
    {
        var repository = new InMemoryRepository();
        var own = RegisterApproved("Acme Ltd", "acme-ltd", "maria@acme.com", "39053344705");
        var other = RegisterApproved("Other Ltd", "other-ltd", "other@acme.com", "52998224725");
        repository.Add(own);
        repository.Add(other);
        return (repository, own, other);
    }

    private static Tenant RegisterApproved(string name, string slug, string email, string document)
    {
        var tenant = Tenant.Register(
            name,
            slug,
            email,
            "11987654321",
            document,
            Address.Create("01310100", "Av Paulista", "100", null, "Bela Vista", "São Paulo", "SP"),
            "Acme",
            "Widgets");
        tenant.Approve();
        return tenant;
    }

    private static ICurrentUser OwnTenantUser(Guid tenantId) =>
        new FakeCurrentUser { TenantId = tenantId, IsPlatformAdmin = false, IsAuthenticated = true };

    private static ICurrentUser PlatformAdminUser() =>
        new FakeCurrentUser { TenantId = null, IsPlatformAdmin = true, IsAuthenticated = true };

    private static UpdateRegistrationByTenantIdCommand ValidUpdateByTenantId(
        Guid tenantId,
        string name,
        string email,
        string document = "39053344705") =>
        new(
            tenantId,
            name,
            email,
            "11987654321",
            document,
            new AddressDto("01310100", "Av Paulista", "100", null, "Bela Vista", "São Paulo", "SP"),
            "Acme",
            "Widgets");

    private static SubmitRegistrationCommand ValidSubmit() =>
        new(
            "Acme Ltd",
            "maria@acme.com",
            "11987654321",
            "39053344705",
            new AddressDto("01310100", "Av Paulista", "100", null, "Bela Vista", "São Paulo", "SP"),
            "Acme",
            "Widgets");
}

file sealed class FakeCurrentUser : ICurrentUser
{
    public Guid? UserId { get; init; } = Guid.NewGuid();

    public string? Email { get; init; } = "user@example.com";

    public Guid? TenantId { get; init; }

    public bool IsPlatformAdmin { get; init; }

    public bool IsAuthenticated { get; init; } = true;

    public IReadOnlyCollection<string> Permissions { get; init; } = [];
}

file sealed class FakeTenantContext : ITenantContext
{
    public Guid? TenantId { get; private set; }

    public bool IsPlatformAdmin { get; private set; }

    public bool IgnoreTenantFilters { get; private set; }

    public bool IgnoreSoftDeleteFilters { get; private set; }

    public void SetTenant(Guid? tenantId, bool isPlatformAdmin = false)
    {
        TenantId = tenantId;
        IsPlatformAdmin = isPlatformAdmin;
    }

    public void SetIgnoreTenantFilters(bool ignore) => IgnoreTenantFilters = ignore;

    public void SetIgnoreSoftDeleteFilters(bool ignore) => IgnoreSoftDeleteFilters = ignore;
}

file sealed class FakeIdentityService : IIdentityService
{
    public Guid NextUserId { get; } = Guid.NewGuid();

    public List<(string Email, string Password, string? DisplayName)> CreatedUsers { get; } = [];

    public Task<IdentityUserInfo?> FindByEmailAsync(string email, CancellationToken cancellationToken = default)
        => Task.FromResult<IdentityUserInfo?>(null);

    public Task<IdentityUserInfo?> FindByIdAsync(Guid userId, CancellationToken cancellationToken = default)
        => Task.FromResult<IdentityUserInfo?>(null);

    public Task<bool> CheckPasswordAsync(Guid userId, string password, CancellationToken cancellationToken = default)
        => Task.FromResult(false);

    public Task<(bool Succeeded, Guid? UserId, IReadOnlyList<string> Errors)> CreateUserAsync(
        string email,
        string password,
        string? displayName,
        bool isPlatformAdmin,
        CancellationToken cancellationToken = default)
    {
        CreatedUsers.Add((email, password, displayName));
        return Task.FromResult<(bool, Guid?, IReadOnlyList<string>)>((true, NextUserId, []));
    }

    public Task<(bool Succeeded, IReadOnlyList<string> Errors)> UpdateUserAsync(
        Guid userId,
        string? displayName,
        bool? isActive,
        CancellationToken cancellationToken = default)
        => Task.FromResult<(bool, IReadOnlyList<string>)>((true, []));

    public Task<(IReadOnlyList<IdentityUserInfo> Items, int TotalCount)> ListUsersAsync(
        int skip,
        int take,
        CancellationToken cancellationToken = default)
        => Task.FromResult<(IReadOnlyList<IdentityUserInfo>, int)>(([], 0));
}

file sealed class InMemoryRepository : IGenericRepository
{
    private readonly Dictionary<Type, List<Entity>> _sets = [];

    private List<Entity> Bucket<T>()
        where T : Entity
    {
        if (!_sets.TryGetValue(typeof(T), out var list))
        {
            list = [];
            _sets[typeof(T)] = list;
        }

        return list;
    }

    public IQueryable<T> Query<T>(bool asNoTracking = true)
        where T : Entity
        => new TestAsyncEnumerable<T>(Bucket<T>().Cast<T>());

    public Task<T?> FirstOrDefaultAsync<T>(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
        where T : Entity
        => Task.FromResult(Bucket<T>().Cast<T>().AsQueryable().FirstOrDefault(predicate));

    public Task<bool> AnyAsync<T>(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
        where T : Entity
        => Task.FromResult(Bucket<T>().Cast<T>().AsQueryable().Any(predicate));

    public void Add<T>(T entity)
        where T : Entity
        => Bucket<T>().Add(entity);

    public Task<T?> GetByIdAsync<T>(Guid id, CancellationToken cancellationToken = default)
        where T : Entity
        => Task.FromResult(Bucket<T>().Cast<T>().FirstOrDefault(e => e.Id == id));

    public Task InsertAsync<T>(T entity, CancellationToken cancellationToken = default)
        where T : Entity
    {
        Add(entity);
        return Task.CompletedTask;
    }

    public Task UpsertAsync<T>(T entity, CancellationToken cancellationToken = default)
        where T : Entity => throw new NotSupportedException();

    public Task DeleteAsync<T>(T entity, CancellationToken cancellationToken = default)
        where T : Entity => throw new NotSupportedException();

    public Task BulkInsertAsync<T>(IList<T> entities, CancellationToken cancellationToken = default)
        where T : Entity => throw new NotSupportedException();

    public Task BulkInsertOrUpdateAsync<T>(IList<T> entities, CancellationToken cancellationToken = default)
        where T : Entity => throw new NotSupportedException();

    public Task BulkUpsertAsync<T>(IList<T> entities, CancellationToken cancellationToken = default)
        where T : Entity => throw new NotSupportedException();

    public Task BulkUpdateAsync<T>(IList<T> entities, CancellationToken cancellationToken = default)
        where T : Entity => throw new NotSupportedException();

    public Task BulkDeleteAsync<T>(IList<T> entities, CancellationToken cancellationToken = default)
        where T : Entity => throw new NotSupportedException();

    public Task<int> CommitAsync(CancellationToken cancellationToken = default) => Task.FromResult(0);
}

file sealed class TestAsyncEnumerable<T> : EnumerableQuery<T>, IAsyncEnumerable<T>, IQueryable<T>
{
    public TestAsyncEnumerable(IEnumerable<T> enumerable)
        : base(enumerable)
    {
    }

    public TestAsyncEnumerable(Expression expression)
        : base(expression)
    {
    }

    IQueryProvider IQueryable.Provider => new TestAsyncQueryProvider<T>(this);

    public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        => new TestAsyncEnumerator<T>(this.AsEnumerable().GetEnumerator());
}

file sealed class TestAsyncQueryProvider<T> : IAsyncQueryProvider
{
    private readonly IQueryProvider _inner;

    public TestAsyncQueryProvider(IQueryProvider inner) => _inner = inner;

    public IQueryable CreateQuery(Expression expression) => new TestAsyncEnumerable<T>(expression);

    public IQueryable<TElement> CreateQuery<TElement>(Expression expression) =>
        new TestAsyncEnumerable<TElement>(expression);

    public object? Execute(Expression expression) => _inner.Execute(expression);

    public TResult Execute<TResult>(Expression expression) => _inner.Execute<TResult>(expression);

    public TResult ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken = default)
    {
        var resultType = typeof(TResult).GetGenericArguments()[0];
        var execute = typeof(IQueryProvider)
            .GetMethod(nameof(IQueryProvider.Execute), 1, [typeof(Expression)])!
            .MakeGenericMethod(resultType)
            .Invoke(_inner, [expression]);

        return (TResult)typeof(Task).GetMethod(nameof(Task.FromResult))!
            .MakeGenericMethod(resultType)
            .Invoke(null, [execute])!;
    }
}

file sealed class TestAsyncEnumerator<T>(IEnumerator<T> inner) : IAsyncEnumerator<T>
{
    public T Current => inner.Current;

    public ValueTask DisposeAsync()
    {
        inner.Dispose();
        return ValueTask.CompletedTask;
    }

    public ValueTask<bool> MoveNextAsync() => new(inner.MoveNext());
}
