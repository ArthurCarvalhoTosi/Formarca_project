using FluentValidation;
using Formarca.Application.Common.Exceptions;
using Formarca.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Formarca.Application.IdentityAccess.Auth.Commands;

public sealed record LogoutCommand(string RefreshToken) : IRequest;

public sealed class LogoutCommandValidator : AbstractValidator<LogoutCommand>
{
    public LogoutCommandValidator()
    {
        RuleFor(x => x.RefreshToken).NotEmpty();
    }
}

public sealed class LogoutCommandHandler(
    IApplicationDbContext db,
    IJwtTokenService jwt,
    ITenantContext tenantContext) : IRequestHandler<LogoutCommand>
{
    public async Task Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        tenantContext.SetIgnoreTenantFilters(true);

        var hash = jwt.HashToken(request.RefreshToken);
        var existing = await db.RefreshTokens
            .FirstOrDefaultAsync(x => x.TokenHash == hash && !x.IsDeleted, cancellationToken);

        if (existing is null)
        {
            return;
        }

        if (existing.IsActive)
        {
            existing.Revoke();
            await db.SaveChangesAsync(cancellationToken);
        }
    }
}
