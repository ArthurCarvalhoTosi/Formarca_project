using FluentValidation;
using Formarca.Application.Common.Exceptions;
using Formarca.Application.Common.Interfaces;
using Formarca.Application.Common.Models;
using Formarca.Domain.IdentityAccess;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Formarca.Application.IdentityAccess.TableViews.Commands;

public sealed record CreateUserTableViewCommand(
    string TableKey,
    string Name,
    IReadOnlyList<string> ColumnKeys,
    bool Activate = false) : IRequest<UserTableViewDto>;

public sealed class CreateUserTableViewCommandValidator : AbstractValidator<CreateUserTableViewCommand>
{
    public CreateUserTableViewCommandValidator()
    {
        RuleFor(x => x.TableKey)
            .NotEmpty()
            .Must(TableViewCatalog.IsKnownTableKey)
            .WithMessage("Unknown table key.");
        RuleFor(x => x.Name).NotEmpty().MaximumLength(UserTableView.NameMaxLength);
        RuleFor(x => x.ColumnKeys).NotEmpty();
        RuleForEach(x => x.ColumnKeys).NotEmpty();
    }
}

public sealed class CreateUserTableViewCommandHandler(
    ICurrentUser currentUser,
    IGenericRepository repository) : IRequestHandler<CreateUserTableViewCommand, UserTableViewDto>
{
    public async Task<UserTableViewDto> Handle(CreateUserTableViewCommand request, CancellationToken cancellationToken)
    {
        var userId = TableViewCommands.RequireUserId(currentUser);
        TableViewCommands.EnsureKnownTableKey(request.TableKey);

        var existing = await TableViewCommands.LoadForTableAsync(
            repository, userId, request.TableKey, cancellationToken);

        var name = request.Name.Trim();
        if (existing.Any(v => string.Equals(v.Name, name, StringComparison.OrdinalIgnoreCase)))
        {
            throw new ConflictException("A view with this name already exists.");
        }

        try
        {
            var view = UserTableView.Create(
                userId,
                request.TableKey,
                name,
                request.ColumnKeys,
                request.Activate,
                existing);
            repository.Add(view);
            await repository.CommitAsync(cancellationToken);
            return TableViewCommands.ToDto(view);
        }
        catch (ArgumentException ex)
        {
            throw new ConflictException(ex.Message);
        }
    }
}

public sealed record UpdateUserTableViewCommand(
    Guid Id,
    string? Name,
    IReadOnlyList<string>? ColumnKeys) : IRequest<UserTableViewDto>;

public sealed class UpdateUserTableViewCommandValidator : AbstractValidator<UpdateUserTableViewCommand>
{
    public UpdateUserTableViewCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        When(x => x.Name is not null, () =>
        {
            RuleFor(x => x.Name!).NotEmpty().MaximumLength(UserTableView.NameMaxLength);
        });
        When(x => x.ColumnKeys is not null, () =>
        {
            RuleFor(x => x.ColumnKeys!).NotEmpty();
            RuleForEach(x => x.ColumnKeys!).NotEmpty();
        });
    }
}

public sealed class UpdateUserTableViewCommandHandler(
    ICurrentUser currentUser,
    IGenericRepository repository) : IRequestHandler<UpdateUserTableViewCommand, UserTableViewDto>
{
    public async Task<UserTableViewDto> Handle(UpdateUserTableViewCommand request, CancellationToken cancellationToken)
    {
        var userId = TableViewCommands.RequireUserId(currentUser);
        var view = await TableViewCommands.LoadOwnedAsync(repository, userId, request.Id, cancellationToken);

        if (request.Name is not null)
        {
            var name = request.Name.Trim();
            var nameTaken = await repository.AnyAsync<UserTableView>(
                v => v.UserId == userId
                    && v.TableKey == view.TableKey
                    && v.Id != view.Id
                    && !v.IsDeleted
                    && v.Name == name,
                cancellationToken);
            if (nameTaken)
            {
                throw new ConflictException("A view with this name already exists.");
            }

            try
            {
                view.Rename(name);
            }
            catch (ArgumentException ex)
            {
                throw new ConflictException(ex.Message);
            }
        }

        if (request.ColumnKeys is not null)
        {
            try
            {
                view.SetColumnKeys(request.ColumnKeys);
            }
            catch (ArgumentException ex)
            {
                throw new ConflictException(ex.Message);
            }
        }

        await repository.CommitAsync(cancellationToken);
        return TableViewCommands.ToDto(view);
    }
}

public sealed record DeleteUserTableViewCommand(Guid Id) : IRequest;

public sealed class DeleteUserTableViewCommandValidator : AbstractValidator<DeleteUserTableViewCommand>
{
    public DeleteUserTableViewCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public sealed class DeleteUserTableViewCommandHandler(
    ICurrentUser currentUser,
    IGenericRepository repository) : IRequestHandler<DeleteUserTableViewCommand>
{
    public async Task Handle(DeleteUserTableViewCommand request, CancellationToken cancellationToken)
    {
        var userId = TableViewCommands.RequireUserId(currentUser);
        var view = await TableViewCommands.LoadOwnedAsync(repository, userId, request.Id, cancellationToken);
        var siblings = await TableViewCommands.LoadForTableAsync(
            repository, userId, view.TableKey, cancellationToken);

        view.Delete(siblings);
        await repository.CommitAsync(cancellationToken);
    }
}

public sealed record ActivateUserTableViewCommand(Guid Id) : IRequest<UserTableViewDto>;

public sealed class ActivateUserTableViewCommandValidator : AbstractValidator<ActivateUserTableViewCommand>
{
    public ActivateUserTableViewCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public sealed class ActivateUserTableViewCommandHandler(
    ICurrentUser currentUser,
    IGenericRepository repository) : IRequestHandler<ActivateUserTableViewCommand, UserTableViewDto>
{
    public async Task<UserTableViewDto> Handle(ActivateUserTableViewCommand request, CancellationToken cancellationToken)
    {
        var userId = TableViewCommands.RequireUserId(currentUser);
        var view = await TableViewCommands.LoadOwnedAsync(repository, userId, request.Id, cancellationToken);
        var siblings = await TableViewCommands.LoadForTableAsync(
            repository, userId, view.TableKey, cancellationToken);

        view.Activate(siblings);
        await repository.CommitAsync(cancellationToken);
        return TableViewCommands.ToDto(view);
    }
}

internal static class TableViewCommands
{
    public static Guid RequireUserId(ICurrentUser currentUser)
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId is null || currentUser.UserId == Guid.Empty)
        {
            throw new UnauthorizedAppException("Not authenticated.");
        }

        return currentUser.UserId.Value;
    }

    public static void EnsureKnownTableKey(string tableKey)
    {
        if (!TableViewCatalog.IsKnownTableKey(tableKey))
        {
            throw new ConflictException("Unknown table key.");
        }
    }

    public static async Task<UserTableView> LoadOwnedAsync(
        IGenericRepository repository,
        Guid userId,
        Guid id,
        CancellationToken cancellationToken)
        => await repository.FirstOrDefaultAsync<UserTableView>(
            v => v.Id == id && v.UserId == userId && !v.IsDeleted, cancellationToken)
            ?? throw new NotFoundException("Table view not found.");

    public static Task<List<UserTableView>> LoadForTableAsync(
        IGenericRepository repository,
        Guid userId,
        string tableKey,
        CancellationToken cancellationToken)
        => repository.Query<UserTableView>(asNoTracking: false)
            .Where(v => v.UserId == userId && v.TableKey == tableKey && !v.IsDeleted)
            .ToListAsync(cancellationToken);

    public static UserTableViewDto ToDto(UserTableView view) =>
        new(
            view.Id,
            view.TableKey,
            view.Name,
            view.GetColumnKeys(),
            view.IsActive,
            view.CreatedAtUtc,
            view.UpdatedAtUtc);
}
