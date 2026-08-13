using FluentValidation;
using Formarca.Application.Common.Exceptions;
using Formarca.Application.Common.Interfaces;
using Formarca.Application.Common.Models;
using Formarca.Application.IdentityAccess.Auth;
using Formarca.Domain.IdentityAccess;
using Formarca.Domain.Tenancy;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Formarca.Application.IdentityAccess.Users.Commands;

public sealed record CreateUserCommand(
    string Email,
    string Password,
    string? DisplayName,
    bool IsPlatformAdmin,
    Guid? TenantId,
    Guid? GroupId) : IRequest<UserDto>;

public sealed class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8);
    }
}

public sealed class CreateUserCommandHandler(
    IIdentityService identity,
    IGenericRepository repository,
    ITenantContext tenantContext) : IRequestHandler<CreateUserCommand, UserDto>
{
    public async Task<UserDto> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        tenantContext.SetIgnoreTenantFilters(true);

        var (succeeded, userId, errors) = await identity.CreateUserAsync(
            request.Email,
            request.Password,
            request.DisplayName,
            request.IsPlatformAdmin,
            cancellationToken);

        if (!succeeded || userId is null)
        {
            throw new ConflictException(string.Join("; ", errors));
        }

        if (request.TenantId is not null)
        {
            var tenantExists = await repository.AnyAsync<Tenant>(t => t.Id == request.TenantId && !t.IsDeleted, cancellationToken);
            if (!tenantExists)
            {
                throw new NotFoundException("Tenant not found.");
            }

            repository.Add(UserTenantMembership.Create(userId.Value, request.TenantId.Value, isDefault: true));

            if (request.GroupId is not null)
            {
                var groupOk = await repository.AnyAsync<Group>(
                    g => g.Id == request.GroupId && (g.TenantId == request.TenantId || g.TenantId == null) && !g.IsDeleted,
                    cancellationToken);
                if (!groupOk)
                {
                    throw new NotFoundException("Group not found for tenant.");
                }

                repository.Add(
                    UserGroupMembership.Create(userId.Value, request.TenantId.Value, request.GroupId.Value));
            }

            await repository.CommitAsync(cancellationToken);
        }

        var tenants = await AuthTokenIssuer.GetUserTenantsAsync(repository, userId.Value, cancellationToken);
        return new UserDto(userId.Value, request.Email, request.DisplayName, request.IsPlatformAdmin, true, tenants);
    }
}

public sealed record AssignUserToTenantCommand(Guid UserId, Guid TenantId, bool IsDefault, Guid? GroupId) : IRequest;

public sealed class AssignUserToTenantCommandHandler(
    IIdentityService identity,
    IGenericRepository repository,
    ITenantContext tenantContext) : IRequestHandler<AssignUserToTenantCommand>
{
    public async Task Handle(AssignUserToTenantCommand request, CancellationToken cancellationToken)
    {
        tenantContext.SetIgnoreTenantFilters(true);

        _ = await identity.FindByIdAsync(request.UserId, cancellationToken)
            ?? throw new NotFoundException("User not found.");

        _ = await repository.FirstOrDefaultAsync<Tenant>(t => t.Id == request.TenantId && !t.IsDeleted, cancellationToken)
            ?? throw new NotFoundException("Tenant not found.");

        var existing = await repository.FirstOrDefaultAsync<UserTenantMembership>(
            m => m.UserId == request.UserId && m.TenantId == request.TenantId && !m.IsDeleted, cancellationToken);

        if (existing is null)
        {
            repository.Add(UserTenantMembership.Create(request.UserId, request.TenantId, request.IsDefault));
        }
        else
        {
            existing.SetActive(true);
            existing.SetDefault(request.IsDefault);
        }

        if (request.IsDefault)
        {
            var others = await repository.Query<UserTenantMembership>(asNoTracking: false)
                .Where(m => m.UserId == request.UserId && m.TenantId != request.TenantId && !m.IsDeleted)
                .ToListAsync(cancellationToken);
            foreach (var other in others)
            {
                other.SetDefault(false);
            }
        }

        if (request.GroupId is not null)
        {
            var already = await repository.AnyAsync<UserGroupMembership>(
                g => g.UserId == request.UserId && g.TenantId == request.TenantId && g.GroupId == request.GroupId && !g.IsDeleted,
                cancellationToken);
            if (!already)
            {
                repository.Add(UserGroupMembership.Create(request.UserId, request.TenantId, request.GroupId.Value));
            }
        }

        await repository.CommitAsync(cancellationToken);
    }
}

public sealed record RemoveUserFromTenantCommand(Guid UserId, Guid TenantId) : IRequest;

public sealed class RemoveUserFromTenantCommandHandler(
    IGenericRepository repository,
    ITenantContext tenantContext) : IRequestHandler<RemoveUserFromTenantCommand>
{
    public async Task Handle(RemoveUserFromTenantCommand request, CancellationToken cancellationToken)
    {
        tenantContext.SetIgnoreTenantFilters(true);

        var membership = await repository.FirstOrDefaultAsync<UserTenantMembership>(
            m => m.UserId == request.UserId && m.TenantId == request.TenantId && !m.IsDeleted, cancellationToken)
            ?? throw new NotFoundException("Membership not found.");

        membership.SoftDelete();

        var groups = await repository.Query<UserGroupMembership>(asNoTracking: false)
            .Where(g => g.UserId == request.UserId && g.TenantId == request.TenantId && !g.IsDeleted)
            .ToListAsync(cancellationToken);
        foreach (var g in groups)
        {
            g.SoftDelete();
        }

        await repository.CommitAsync(cancellationToken);
    }
}
