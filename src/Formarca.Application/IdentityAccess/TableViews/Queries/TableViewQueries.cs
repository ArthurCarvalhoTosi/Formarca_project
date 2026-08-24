using FluentValidation;
using Formarca.Application.Common.Interfaces;
using Formarca.Application.Common.Models;
using Formarca.Application.IdentityAccess.TableViews.Commands;
using Formarca.Domain.IdentityAccess;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Formarca.Application.IdentityAccess.TableViews.Queries;

public sealed record ListUserTableViewsQuery(string TableKey) : IRequest<IReadOnlyList<UserTableViewDto>>;

public sealed class ListUserTableViewsQueryValidator : AbstractValidator<ListUserTableViewsQuery>
{
    public ListUserTableViewsQueryValidator()
    {
        RuleFor(x => x.TableKey)
            .NotEmpty()
            .Must(TableViewCatalog.IsKnownTableKey)
            .WithMessage("Unknown table key.");
    }
}

public sealed class ListUserTableViewsQueryHandler(
    ICurrentUser currentUser,
    IGenericRepository repository) : IRequestHandler<ListUserTableViewsQuery, IReadOnlyList<UserTableViewDto>>
{
    public async Task<IReadOnlyList<UserTableViewDto>> Handle(
        ListUserTableViewsQuery request,
        CancellationToken cancellationToken)
    {
        var userId = TableViewCommands.RequireUserId(currentUser);
        TableViewCommands.EnsureKnownTableKey(request.TableKey);

        var views = await repository.Query<UserTableView>()
            .Where(v => v.UserId == userId && v.TableKey == request.TableKey && !v.IsDeleted)
            .OrderByDescending(v => v.IsActive)
            .ThenBy(v => v.Name)
            .ToListAsync(cancellationToken);

        return views.Select(TableViewCommands.ToDto).ToList();
    }
}
