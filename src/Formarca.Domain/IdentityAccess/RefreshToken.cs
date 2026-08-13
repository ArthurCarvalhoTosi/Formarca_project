using Formarca.Domain.Common;

namespace Formarca.Domain.IdentityAccess;

public class RefreshToken : Entity
{
    private RefreshToken()
    {
    }

    public Guid UserId { get; private set; }

    public string TokenHash { get; private set; } = string.Empty;

    public DateTime ExpiresAtUtc { get; private set; }

    public DateTime? RevokedAtUtc { get; private set; }

    public string? ReplacedByTokenHash { get; private set; }

    public Guid? TenantId { get; private set; }

    public bool IsActive => RevokedAtUtc is null && ExpiresAtUtc > Clock.Current.UtcNow;

    public static RefreshToken Create(Guid userId, string tokenHash, DateTime expiresAtUtc, Guid? tenantId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tokenHash);

        return new RefreshToken
        {
            UserId = userId,
            TokenHash = tokenHash,
            ExpiresAtUtc = expiresAtUtc,
            TenantId = tenantId
        };
    }

    public void Revoke(string? replacedByTokenHash = null)
    {
        RevokedAtUtc = Clock.Current.UtcNow;
        ReplacedByTokenHash = replacedByTokenHash;
        MarkUpdated();
    }
}
