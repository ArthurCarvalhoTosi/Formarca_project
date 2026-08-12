using FluentValidation;
using Formarca.Application.Common.Exceptions;
using Formarca.Application.Common.Interfaces;
using Formarca.Application.Common.Models;
using Formarca.Application.IdentityAccess.Auth;
using Formarca.Domain.IdentityAccess;
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
    IApplicationDbContext db,
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
            var tenantExists = await db.Tenants.AnyAsync(t => t.Id == request.TenantId && !t.IsDeleted, cancellationToken);
            if (!tenantExists)
            {
                throw new NotFoundException("Tenant not found.");
            }

            db.UserTenantMemberships.Add(UserTenantMembership.Create(userId.Value, request.TenantId.Value, isDefault: true));

            if (request.GroupId is not null)
            {
                var groupOk = await db.Groups.AnyAsync(
                    g => g.Id == request.GroupId && (g.TenantId == request.TenantId || g.TenantId == null) && !g.IsDeleted,
                    cancellationToken);
                if (!groupOk)
                {
                    throw new NotFoundException("Group not found for tenant.");
                }

                db.UserGroupMemberships.Add(
                    UserGroupMembership.Create(userId.Value, request.TenantId.Value, request.GroupId.Value));
            }

            await db.SaveChangesAsync(cancellationToken);
        }

        var tenants = await AuthTokenIssuer.GetUserTenantsAsync(db, userId.Value, cancellationToken);
        return new UserDto(userId.Value, request.Email, request.DisplayName, request.IsPlatformAdmin, true, tenants);
    }
}

public sealed record AssignUserToTenantCommand(Guid UserId, Guid TenantId, bool IsDefault, Guid? GroupId) : IRequest;

public sealed class AssignUserToTenantCommandHandler(
    IIdentityService identity,
    IApplicationDbContext db,
    ITenantContext tenantContext) : IRequestHandler<AssignUserToTenantCommand>
{
    public async Task Handle(AssignUserToTenantCommand request, CancellationToken cancellationToken)
    {
        tenantContext.SetIgnoreTenantFilters(true);

        _ = await identity.FindByIdAsync(request.UserId, cancellationToken)
            ?? throw new NotFoundException("User not found.");

        _ = await db.Tenants.FirstOrDefaultAsync(t => t.Id == request.TenantId && !t.IsDeleted, cancellationToken)
            ?? throw new NotFoundException("Tenant not found.");

        var existing = await db.UserTenantMemberships
            .FirstOrDefaultAsync(m => m.UserId == request.UserId && m.TenantId == request.TenantId && !m.IsDeleted, cancellationToken);

        if (existing is null)
        {
            db.UserTenantMemberships.Add(UserTenantMembership.Create(request.UserId, request.TenantId, request.IsDefault));
        }
        else
        {
            existing.SetActive(true);
            existing.SetDefault(request.IsDefault);
        }

        if (request.IsDefault)
        {
            var others = await db.UserTenantMemberships
                .Where(m => m.UserId == request.UserId && m.TenantId != request.TenantId && !m.IsDeleted)
                .ToListAsync(cancellationToken);
            foreach (var other in others)
            {
                other.SetDefault(false);
            }
        }

        if (request.GroupId is not null)
        {
            var already = await db.UserGroupMemberships.AnyAsync(
                g => g.UserId == request.UserId && g.TenantId == request.TenantId && g.GroupId == request.GroupId && !g.IsDeleted,
                cancellationToken);
            if (!already)
            {
                db.UserGroupMemberships.Add(UserGroupMembership.Create(request.UserId, request.TenantId, request.GroupId.Value));
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}

public sealed record RemoveUserFromTenantCommand(Guid UserId, Guid TenantId) : IRequest;

public sealed class RemoveUserFromTenantCommandHandler(
    IApplicationDbContext db,
    ITenantContext tenantContext) : IRequestHandler<RemoveUserFromTenantCommand>
{
    public async Task Handle(RemoveUserFromTenantCommand request, CancellationToken cancellationToken)
    {
        tenantContext.SetIgnoreTenantFilters(true);

        var membership = await db.UserTenantMemberships
            .FirstOrDefaultAsync(m => m.UserId == request.UserId && m.TenantId == request.TenantId && !m.IsDeleted, cancellationToken)
            ?? throw new NotFoundException("Membership not found.");

        membership.SoftDelete();

        var groups = await db.UserGroupMemberships
            .Where(g => g.UserId == request.UserId && g.TenantId == request.TenantId && !g.IsDeleted)
            .ToListAsync(cancellationToken);
        foreach (var g in groups)
        {
            g.SoftDelete();
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
