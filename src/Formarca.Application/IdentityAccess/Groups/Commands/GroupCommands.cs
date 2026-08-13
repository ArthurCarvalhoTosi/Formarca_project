using FluentValidation;
using Formarca.Application.Common.Exceptions;
using Formarca.Application.Common.Interfaces;
using Formarca.Application.Common.Models;
using Formarca.Domain.IdentityAccess;
using Formarca.Domain.Tenancy;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Formarca.Application.IdentityAccess.Groups.Commands;

public sealed record CreateGroupCommand(
    string Name,
    Guid? TenantId,
    string? Description,
    IReadOnlyList<string>? PermissionCodes) : IRequest<GroupDto>;

public sealed class CreateGroupCommandValidator : AbstractValidator<CreateGroupCommand>
{
    public CreateGroupCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
    }
}

public sealed class CreateGroupCommandHandler(
    IGenericRepository repository,
    ITenantContext tenantContext) : IRequestHandler<CreateGroupCommand, GroupDto>
{
    public async Task<GroupDto> Handle(CreateGroupCommand request, CancellationToken cancellationToken)
    {
        tenantContext.SetIgnoreTenantFilters(true);

        if (request.TenantId is not null)
        {
            var exists = await repository.AnyAsync<Tenant>(t => t.Id == request.TenantId && !t.IsDeleted, cancellationToken);
            if (!exists)
            {
                throw new NotFoundException("Tenant not found.");
            }
        }

        var group = Group.Create(request.Name, request.TenantId, request.Description);
        repository.Add(group);
        await repository.CommitAsync(cancellationToken);

        if (request.PermissionCodes is { Count: > 0 })
        {
            var permissions = await repository.Query<Permission>()
                .Where(p => request.PermissionCodes.Contains(p.Code) && !p.IsDeleted)
                .ToListAsync(cancellationToken);

            foreach (var permission in permissions)
            {
                repository.Add(GroupPermission.Create(group.Id, permission.Id));
            }

            await repository.CommitAsync(cancellationToken);
        }

        return await MapGroupAsync(repository, group.Id, cancellationToken);
    }

    internal static async Task<GroupDto> MapGroupAsync(IGenericRepository repository, Guid groupId, CancellationToken ct)
    {
        var group = await repository.Query<Group>().FirstAsync(g => g.Id == groupId, ct);
        var codes = await (
            from gp in repository.Query<GroupPermission>()
            join p in repository.Query<Permission>() on gp.PermissionId equals p.Id
            where gp.GroupId == groupId && !gp.IsDeleted && !p.IsDeleted
            select p.Code
        ).ToListAsync(ct);

        return new GroupDto(group.Id, group.TenantId, group.Name, group.Description, codes);
    }
}

public sealed record UpdateGroupCommand(
    Guid Id,
    string Name,
    string? Description,
    IReadOnlyList<string>? PermissionCodes) : IRequest<GroupDto>;

public sealed class UpdateGroupCommandValidator : AbstractValidator<UpdateGroupCommand>
{
    public UpdateGroupCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
    }
}

public sealed class UpdateGroupCommandHandler(
    IGenericRepository repository,
    ITenantContext tenantContext) : IRequestHandler<UpdateGroupCommand, GroupDto>
{
    public async Task<GroupDto> Handle(UpdateGroupCommand request, CancellationToken cancellationToken)
    {
        tenantContext.SetIgnoreTenantFilters(true);

        var group = await repository.FirstOrDefaultAsync<Group>(
            g => g.Id == request.Id && !g.IsDeleted, cancellationToken)
            ?? throw new NotFoundException("Group not found.");

        group.Update(request.Name, request.Description);

        if (request.PermissionCodes is not null)
        {
            var existing = await repository.Query<GroupPermission>(asNoTracking: false)
                .Where(gp => gp.GroupId == group.Id && !gp.IsDeleted)
                .ToListAsync(cancellationToken);

            foreach (var item in existing)
            {
                item.SoftDelete();
            }

            var permissions = await repository.Query<Permission>()
                .Where(p => request.PermissionCodes.Contains(p.Code) && !p.IsDeleted)
                .Select(p => p.Id)
                .ToListAsync(cancellationToken);

            foreach (var permissionId in permissions)
            {
                repository.Add(GroupPermission.Create(group.Id, permissionId));
            }
        }

        await repository.CommitAsync(cancellationToken);
        return await CreateGroupCommandHandler.MapGroupAsync(repository, group.Id, cancellationToken);
    }
}

public sealed record DeleteGroupCommand(Guid Id) : IRequest;

public sealed class DeleteGroupCommandHandler(
    IGenericRepository repository,
    ITenantContext tenantContext) : IRequestHandler<DeleteGroupCommand>
{
    public async Task Handle(DeleteGroupCommand request, CancellationToken cancellationToken)
    {
        tenantContext.SetIgnoreTenantFilters(true);

        var group = await repository.FirstOrDefaultAsync<Group>(g => g.Id == request.Id && !g.IsDeleted, cancellationToken)
            ?? throw new NotFoundException("Group not found.");

        group.SoftDelete();
        await repository.CommitAsync(cancellationToken);
    }
}

public sealed record AssignUserToGroupCommand(Guid UserId, Guid GroupId, Guid TenantId) : IRequest;

public sealed class AssignUserToGroupCommandHandler(
    IGenericRepository repository,
    ITenantContext tenantContext) : IRequestHandler<AssignUserToGroupCommand>
{
    public async Task Handle(AssignUserToGroupCommand request, CancellationToken cancellationToken)
    {
        tenantContext.SetIgnoreTenantFilters(true);

        var group = await repository.FirstOrDefaultAsync<Group>(
            g => g.Id == request.GroupId && !g.IsDeleted, cancellationToken)
            ?? throw new NotFoundException("Group not found.");

        if (group.TenantId is not null && group.TenantId != request.TenantId)
        {
            throw new ForbiddenException("Group does not belong to the tenant.");
        }

        var membership = await repository.AnyAsync<UserTenantMembership>(
            m => m.UserId == request.UserId && m.TenantId == request.TenantId && m.IsActive && !m.IsDeleted, cancellationToken);
        if (!membership)
        {
            throw new ForbiddenException("User is not a member of the tenant.");
        }

        var exists = await repository.AnyAsync<UserGroupMembership>(
            x => x.UserId == request.UserId && x.GroupId == request.GroupId && x.TenantId == request.TenantId && !x.IsDeleted,
            cancellationToken);

        if (!exists)
        {
            repository.Add(UserGroupMembership.Create(request.UserId, request.TenantId, request.GroupId));
            await repository.CommitAsync(cancellationToken);
        }
    }
}
