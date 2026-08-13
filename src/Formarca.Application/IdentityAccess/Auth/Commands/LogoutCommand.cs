using FluentValidation;
using Formarca.Application.Common.Interfaces;
using Formarca.Domain.IdentityAccess;
using MediatR;

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
    IGenericRepository repository,
    IJwtTokenService jwt,
    ITenantContext tenantContext) : IRequestHandler<LogoutCommand>
{
    public async Task Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        tenantContext.SetIgnoreTenantFilters(true);

        var hash = jwt.HashToken(request.RefreshToken);
        var existing = await repository.FirstOrDefaultAsync<RefreshToken>(
            x => x.TokenHash == hash && !x.IsDeleted, cancellationToken);

        if (existing is null)
        {
            return;
        }

        if (existing.IsActive)
        {
            existing.Revoke();
            await repository.CommitAsync(cancellationToken);
        }
    }
}
