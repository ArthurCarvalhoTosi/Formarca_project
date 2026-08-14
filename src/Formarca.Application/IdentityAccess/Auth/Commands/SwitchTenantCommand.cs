using FluentValidation;
using Formarca.Application.Common.Exceptions;
using Formarca.Application.Common.Interfaces;
using Formarca.Application.Common.Models;
using Formarca.Domain.IdentityAccess;
using Formarca.Domain.Tenancy;
using MediatR;

namespace Formarca.Application.IdentityAccess.Auth.Commands;

public sealed record SwitchTenantCommand(Guid TenantId) : IRequest<AuthResultDto>;

public sealed class SwitchTenantCommandValidator : AbstractValidator<SwitchTenantCommand>
{
    public SwitchTenantCommandValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
    }
}

public sealed class SwitchTenantCommandHandler(
    ICurrentUser currentUser,
    IIdentityService identity,
    IGenericRepository repository,
    IJwtTokenService jwt,
    ITenantContext tenantContext) : IRequestHandler<SwitchTenantCommand, AuthResultDto>
{
    public async Task<AuthResultDto> Handle(SwitchTenantCommand request, CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId is null)
        {
            throw new UnauthorizedAppException("Not authenticated.");
        }

        tenantContext.SetIgnoreTenantFilters(true);

        var user = await identity.FindByIdAsync(currentUser.UserId.Value, cancellationToken)
            ?? throw new UnauthorizedAppException("User not found.");

        var tenant = await repository.FirstOrDefaultAsync<Tenant>(
            t => t.Id == request.TenantId && !t.IsDeleted, cancellationToken)
            ?? throw new NotFoundException("Tenant not found.");

        if (tenant.Status != TenantStatus.Active && !user.IsPlatformAdmin)
        {
            throw new ForbiddenException("Tenant is not active.");
        }

        if (!user.IsPlatformAdmin)
        {
            var membership = await repository.AnyAsync<UserTenantMembership>(
                m => m.UserId == user.Id && m.TenantId == request.TenantId && m.IsActive && !m.IsDeleted,
                cancellationToken);

            if (!membership)
            {
                throw new ForbiddenException("User is not a member of the requested tenant.");
            }
        }

        var tenants = await AuthTokenIssuer.GetUserTenantsAsync(
            repository, user.Id, cancellationToken, includeNonActiveTenants: user.IsPlatformAdmin);
        if (user.IsPlatformAdmin && tenants.All(t => t.Id != tenant.Id))
        {
            tenants = tenants.Append(new TenantSummaryDto(tenant.Id, tenant.Name, tenant.Slug, false)).ToList();
        }

        var (tokens, permissions) = await AuthTokenIssuer.IssueAsync(
            repository, jwt, user.Id, user.Email, user.IsPlatformAdmin, request.TenantId, cancellationToken);

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
