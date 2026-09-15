using FluentValidation;
using Formarca.Application.Common.Exceptions;
using Formarca.Application.Common.Interfaces;
using Formarca.Application.Common.Models;
using Formarca.Domain.Common;
using MediatR;

namespace Formarca.Application.IdentityAccess.Auth.Commands;

public sealed record RefreshTokenCommand(string RefreshToken) : IRequest<AuthResultDto>;

public sealed class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
{
    public RefreshTokenCommandValidator()
    {
        RuleFor(x => x.RefreshToken).NotEmpty();
    }
}

public sealed class RefreshTokenCommandHandler(
    IGenericRepository repository,
    IIdentityService identity,
    IJwtTokenService jwt,
    ITenantContext tenantContext) : IRequestHandler<RefreshTokenCommand, AuthResultDto>
{
    public async Task<AuthResultDto> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        tenantContext.SetIgnoreTenantFilters(true);

        var hash = jwt.HashToken(request.RefreshToken);
        var existing = await repository.FirstOrDefaultAsync<Domain.IdentityAccess.RefreshToken>(
            x => x.TokenHash == hash && !x.IsDeleted, cancellationToken)
            ?? throw new UnauthorizedAppException("Invalid refresh token.");

        if (!existing.IsActive)
        {
            throw new UnauthorizedAppException("Refresh token is expired or revoked.");
        }

        var user = await identity.FindByIdAsync(existing.UserId, cancellationToken)
            ?? throw new UnauthorizedAppException("User not found.");

        if (!user.IsActive)
        {
            throw new UnauthorizedAppException("User is inactive.");
        }

        var newRefresh = jwt.CreateRefreshToken();
        existing.Revoke(newRefresh.TokenHash);

        var tenants = await AuthTokenIssuer.GetUserTenantsAsync(
            repository, user.Id, cancellationToken, includeNonActiveTenants: user.IsPlatformAdmin);
        var tenantId = existing.TenantId;

        var permissions = await AuthTokenIssuer.ResolvePermissionsAsync(
            repository, user.Id, tenantId, user.IsPlatformAdmin, cancellationToken);

        var accessToken = jwt.CreateAccessToken(user.Id, user.Email, tenantId, user.IsPlatformAdmin, permissions);
        repository.Add(RefreshTokenEntity(user.Id, newRefresh.TokenHash, newRefresh.ExpiresAtUtc, tenantId));
        await repository.CommitAsync(cancellationToken);

        var tokens = new AuthTokensDto(accessToken, newRefresh.RawToken, Clock.Current.UtcNow.AddMinutes(30), tenantId);

        return new AuthResultDto(tokens, user.Id, user.Email, user.DisplayName, user.IsPlatformAdmin, tenants, permissions);
    }

    private static Domain.IdentityAccess.RefreshToken RefreshTokenEntity(
        Guid userId, string hash, DateTime expires, Guid? tenantId) =>
        Domain.IdentityAccess.RefreshToken.Create(userId, hash, expires, tenantId);
}
