using FluentValidation;
using Formarca.Application.Common.Exceptions;
using Formarca.Application.Common.Interfaces;
using Formarca.Application.Common.Models;
using Formarca.Domain.Tenancy;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Formarca.Application.IdentityAccess.Auth.Commands;

public sealed record LoginCommand(string Email, string Password, Guid? TenantId = null) : IRequest<AuthResultDto>;

public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty();
    }
}

public sealed class LoginCommandHandler(
    IIdentityService identity,
    IApplicationDbContext db,
    IJwtTokenService jwt,
    ITenantContext tenantContext) : IRequestHandler<LoginCommand, AuthResultDto>
{
    public async Task<AuthResultDto> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        tenantContext.SetIgnoreTenantFilters(true);

        var user = await identity.FindByEmailAsync(request.Email, cancellationToken)
            ?? throw new UnauthorizedAppException("Invalid email or password.");

        if (!user.IsActive)
        {
            throw new UnauthorizedAppException("User is inactive.");
        }

        if (!await identity.CheckPasswordAsync(user.Id, request.Password, cancellationToken))
        {
            throw new UnauthorizedAppException("Invalid email or password.");
        }

        var tenants = await AuthTokenIssuer.GetUserTenantsAsync(db, user.Id, cancellationToken);
        Guid? tenantId = request.TenantId;

        if (tenantId is null)
        {
            tenantId = tenants.FirstOrDefault(t => t.IsDefault)?.Id ?? tenants.FirstOrDefault()?.Id;
        }
        else if (!user.IsPlatformAdmin && tenants.All(t => t.Id != tenantId))
        {
            throw new ForbiddenException("User is not a member of the requested tenant.");
        }

        if (tenantId is not null)
        {
            var tenantExists = await db.Tenants.AsNoTracking()
                .AnyAsync(t => t.Id == tenantId && !t.IsDeleted && t.Status == TenantStatus.Active, cancellationToken);
            if (!tenantExists && !user.IsPlatformAdmin)
            {
                throw new ForbiddenException("Tenant is not available.");
            }
        }

        var (tokens, permissions) = await AuthTokenIssuer.IssueAsync(
            db, jwt, user.Id, user.Email, user.IsPlatformAdmin, tenantId, cancellationToken);

        return new AuthResultDto(
            tokens,
            user.Id,
            user.Email,
            user.DisplayName,
            user.IsPlatformAdmin,
            tenants,
            permissions);
    }
}
