namespace Formarca.Application.Common.Interfaces;

public interface IJwtTokenService
{
    string CreateAccessToken(
        Guid userId,
        string email,
        Guid? tenantId,
        bool isPlatformAdmin,
        IEnumerable<string> permissions);

    (string RawToken, string TokenHash, DateTime ExpiresAtUtc) CreateRefreshToken();

    string HashToken(string rawToken);
}
