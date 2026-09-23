using System.Linq.Expressions;
using Formarca.Application.Autentique;
using Formarca.Application.Common.Interfaces;
using Formarca.Domain.Common;
using Microsoft.EntityFrameworkCore.Query;

namespace Formarca.Application.Tests;

internal sealed class FakeClock(DateTime utcNow) : IClock
{
    public DateTime UtcNow { get; set; } = utcNow;
}

internal sealed class FakeCurrentUser : ICurrentUser
{
    public Guid? UserId { get; init; } = Guid.NewGuid();

    public string? Email { get; init; } = "user@example.com";

    public Guid? TenantId { get; init; }

    public bool IsPlatformAdmin { get; init; }

    public bool IsAuthenticated { get; init; } = true;

    public IReadOnlyCollection<string> Permissions { get; init; } = [];
}

internal sealed class FakeTenantContext : ITenantContext
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

internal sealed class FakeIdentityService : IIdentityService
{
    public Guid NextUserId { get; } = Guid.NewGuid();

    public IdentityUserInfo? User { get; set; }

    public bool PasswordValid { get; set; }

    public bool IsLockedOut { get; set; }

    public int AccessFailedCallCount { get; private set; }

    public int ResetAccessFailedCountCallCount { get; private set; }

    public int CheckPasswordCallCount { get; private set; }

    public List<(string Email, string Password, string? DisplayName)> CreatedUsers { get; } = [];

    public Task<IdentityUserInfo?> FindByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        if (User is not null && string.Equals(User.Email, email, StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult<IdentityUserInfo?>(User);
        }

        return Task.FromResult<IdentityUserInfo?>(null);
    }

    public Task<IdentityUserInfo?> FindByIdAsync(Guid userId, CancellationToken cancellationToken = default)
        => Task.FromResult(User is not null && User.Id == userId ? User : null);

    public Task<bool> CheckPasswordAsync(Guid userId, string password, CancellationToken cancellationToken = default)
    {
        CheckPasswordCallCount++;
        return Task.FromResult(PasswordValid);
    }

    public Task<bool> IsLockedOutAsync(Guid userId, CancellationToken cancellationToken = default)
        => Task.FromResult(IsLockedOut);

    public Task AccessFailedAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        AccessFailedCallCount++;
        return Task.CompletedTask;
    }

    public Task ResetAccessFailedCountAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        ResetAccessFailedCountCallCount++;
        return Task.CompletedTask;
    }

    public Task<(bool Succeeded, Guid? UserId, IReadOnlyList<string> Errors)> CreateUserAsync(
        string email,
        string password,
        string? displayName,
        bool isPlatformAdmin,
        CancellationToken cancellationToken = default)
    {
        CreatedUsers.Add((email, password, displayName));
        User = new IdentityUserInfo(NextUserId, email, displayName, isPlatformAdmin, true, true);
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

internal sealed class FakeJwtTokenService : IJwtTokenService
{
    public string CreateAccessToken(
        Guid userId,
        string email,
        Guid? tenantId,
        bool isPlatformAdmin,
        IEnumerable<string> permissions)
        => "access-token";

    public (string RawToken, string TokenHash, DateTime ExpiresAtUtc) CreateRefreshToken()
        => ("refresh-token", "refresh-hash", DateTime.UtcNow.AddDays(7));

    public string HashToken(string rawToken) => $"hash:{rawToken}";
}

internal sealed class FakeContractDocumentGenerator : IContractDocumentGenerator
{
    public List<ContractDocumentModel> PdfModels { get; } = [];

    public byte[] PdfBytes { get; set; } = "%PDF-1.7 fake"u8.ToArray();

    public byte[] GeneratePdf(ContractDocumentModel model)
    {
        PdfModels.Add(model);
        var stamp = model.ClientSignaturePng is { Length: > 0 } ? "signed"u8.ToArray() : "issued"u8.ToArray();
        return [.. PdfBytes, .. stamp];
    }

    public byte[] GenerateWord(ContractDocumentModel model) => "PK"u8.ToArray();
}

internal sealed class FakeContractTemplateProvider : IContractTemplateProvider
{
    public string Template { get; set; } =
        "Nome {{contratanteNome}} CNPJ {{cnpj}} Rua {{endereco}} {{numero}} {{cidade}}/{{uf}} CEP {{cep}} Marca {{marcaNome}} Classes {{quantidadeClasses}} Valor {{valor}} Periodo {{periodo}}";

    public string GetTemplate(string templateKey) => Template;
}

internal sealed class FakeAutentiqueClient : IAutentiqueClient
{
    public string FormarcaSignerEmail { get; set; } = "formarca@example.com";

    public string WebhookSecret { get; set; } = "webhook-secret";

    public string DocumentId { get; set; } = "autentique-doc-1";

    public string ClientSignerPublicId { get; set; } = "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee";

    public string? SignUrl { get; set; } = "https://assina.ae/fake-link";

    public string RegeneratedSignUrl { get; set; } = "https://assina.ae/regenerated";

    public byte[] SignedPdf { get; set; } = "%PDF-autentique-signed"u8.ToArray();

    public bool UseRealHmac { get; set; } = true;

    public bool VerifyWebhookResult { get; set; } = true;

    public Exception? CreateException { get; set; }

    public List<AutentiqueCreateDocumentRequest> Created { get; } = [];

    public List<string> SignedDocumentIds { get; } = [];

    public List<string> LinkRequests { get; } = [];

    public List<string> Downloads { get; } = [];

    public bool CurrentPdfAvailable { get; set; }

    public byte[]? CurrentPdf { get; set; }

    public bool VerifyWebhookSignature(string rawBody, string? signatureHeader)
    {
        if (UseRealHmac)
        {
            return AutentiqueWebhookHmac.Verify(rawBody, signatureHeader, WebhookSecret);
        }

        return VerifyWebhookResult;
    }

    public Task<AutentiqueCreatedDocument> CreateDocumentAsync(
        AutentiqueCreateDocumentRequest request,
        CancellationToken cancellationToken = default)
    {
        if (CreateException is not null)
        {
            throw CreateException;
        }

        Created.Add(request);
        return Task.FromResult(new AutentiqueCreatedDocument(DocumentId, ClientSignerPublicId, SignUrl));
    }

    public Task SignDocumentAsync(string documentId, CancellationToken cancellationToken = default)
    {
        SignedDocumentIds.Add(documentId);
        return Task.CompletedTask;
    }

    public Task<string> CreateLinkToSignatureAsync(string signerPublicId, CancellationToken cancellationToken = default)
    {
        LinkRequests.Add(signerPublicId);
        return Task.FromResult(RegeneratedSignUrl);
    }

    public Task<byte[]> DownloadSignedPdfAsync(string documentId, CancellationToken cancellationToken = default)
    {
        Downloads.Add(documentId);
        return Task.FromResult(SignedPdf);
    }

    public Task<byte[]?> TryDownloadCurrentPdfAsync(string documentId, CancellationToken cancellationToken = default)
    {
        Downloads.Add(documentId);
        if (!CurrentPdfAvailable)
        {
            return Task.FromResult<byte[]?>(null);
        }

        return Task.FromResult<byte[]?>(CurrentPdf ?? SignedPdf);
    }
}

internal sealed class InMemoryRepository : IGenericRepository
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

internal sealed class TestAsyncEnumerable<T> : EnumerableQuery<T>, IAsyncEnumerable<T>, IQueryable<T>
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

internal sealed class TestAsyncQueryProvider<T> : IAsyncQueryProvider
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

internal sealed class TestAsyncEnumerator<T>(IEnumerator<T> inner) : IAsyncEnumerator<T>
{
    public T Current => inner.Current;

    public ValueTask DisposeAsync()
    {
        inner.Dispose();
        return ValueTask.CompletedTask;
    }

    public ValueTask<bool> MoveNextAsync() => new(inner.MoveNext());
}
