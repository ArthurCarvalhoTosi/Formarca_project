using FluentValidation;
using Formarca.Application.Common.Exceptions;
using Formarca.Application.Common.Interfaces;
using Formarca.Application.Common.Models;
using Formarca.Application.IdentityAccess.Auth;
using Formarca.Application.Tenancy;
using Formarca.Domain.IdentityAccess;
using Formarca.Domain.Tenancy;
using MediatR;

namespace Formarca.Application.Tenancy.Commands;

public sealed record ApproveTenantCommand(Guid Id, string Password) : IRequest<TenantDto>;

public sealed class ApproveTenantCommandValidator : AbstractValidator<ApproveTenantCommand>
{
    public ApproveTenantCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8);
    }
}

public sealed class ApproveTenantCommandHandler(
    IGenericRepository repository,
    IIdentityService identity,
    ITenantContext tenantContext) : IRequestHandler<ApproveTenantCommand, TenantDto>
{
    public async Task<TenantDto> Handle(ApproveTenantCommand request, CancellationToken cancellationToken)
    {
        tenantContext.SetIgnoreTenantFilters(true);

        var tenant = await repository.FirstOrDefaultAsync<Tenant>(
            t => t.Id == request.Id && !t.IsDeleted, cancellationToken)
            ?? throw new NotFoundException("Tenant not found.");

        if (tenant.Status != TenantStatus.Pending)
        {
            throw new ConflictException("Tenant is not pending approval.");
        }

        if (string.IsNullOrWhiteSpace(tenant.Email))
        {
            throw new ConflictException("Tenant has no registration email.");
        }

        var existingUser = await identity.FindByEmailAsync(tenant.Email, cancellationToken);
        if (existingUser is not null)
        {
            throw new ConflictException("A user with this email already exists.");
        }

        await AuthTokenIssuer.EnsureDefaultTenantGroupsAsync(repository, tenant.Id, cancellationToken);

        var (succeeded, userId, errors) = await identity.CreateUserAsync(
            tenant.Email,
            request.Password,
            tenant.Name,
            isPlatformAdmin: false,
            cancellationToken);

        if (!succeeded || userId is null)
        {
            throw new ConflictException(string.Join("; ", errors));
        }

        repository.Add(UserTenantMembership.Create(userId.Value, tenant.Id, isDefault: true));

        var onboardingGroup = await repository.FirstOrDefaultAsync<Group>(
            g => g.TenantId == tenant.Id && g.Name == DefaultGroupNames.ClientOnboarding && !g.IsDeleted,
            cancellationToken)
            ?? throw new ConflictException("ClientOnboarding group was not created.");

        repository.Add(UserGroupMembership.Create(userId.Value, tenant.Id, onboardingGroup.Id));

        tenant.Approve();
        await repository.CommitAsync(cancellationToken);

        return tenant.ToTenantDto();
    }
}

public sealed record RejectTenantCommand(Guid Id) : IRequest<TenantDto>;

public sealed class RejectTenantCommandValidator : AbstractValidator<RejectTenantCommand>
{
    public RejectTenantCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public sealed class RejectTenantCommandHandler(
    IGenericRepository repository,
    ITenantContext tenantContext) : IRequestHandler<RejectTenantCommand, TenantDto>
{
    public async Task<TenantDto> Handle(RejectTenantCommand request, CancellationToken cancellationToken)
    {
        tenantContext.SetIgnoreTenantFilters(true);

        var tenant = await repository.FirstOrDefaultAsync<Tenant>(
            t => t.Id == request.Id && !t.IsDeleted, cancellationToken)
            ?? throw new NotFoundException("Tenant not found.");

        if (tenant.Status != TenantStatus.Pending)
        {
            throw new ConflictException("Tenant is not pending approval.");
        }

        tenant.Reject();
        await repository.CommitAsync(cancellationToken);
        return tenant.ToTenantDto();
    }
}
