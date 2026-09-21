using Formarca.Application.Common.Interfaces;
using Formarca.Domain.Processes;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Formarca.Application.Processes.Queries;

public sealed record ListTrademarkStatusesQuery(bool IncludeInactive = false) : IRequest<IReadOnlyList<TrademarkStatusDto>>;

public sealed class ListTrademarkStatusesQueryHandler(IGenericRepository repository)
    : IRequestHandler<ListTrademarkStatusesQuery, IReadOnlyList<TrademarkStatusDto>>
{
    public async Task<IReadOnlyList<TrademarkStatusDto>> Handle(
        ListTrademarkStatusesQuery request,
        CancellationToken cancellationToken)
    {
        var query = repository.Query<TrademarkStatus>().Where(status => !status.IsDeleted);
        if (!request.IncludeInactive)
        {
            query = query.Where(status => status.IsActive);
        }

        var items = await query
            .OrderBy(status => status.SortOrder)
            .ThenBy(status => status.Title)
            .ToListAsync(cancellationToken);

        return items.Select(status => status.ToDto()).ToList();
    }
}
