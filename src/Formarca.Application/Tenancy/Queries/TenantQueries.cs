using Formarca.Application.Common.Exceptions;
using Formarca.Application.Common.Interfaces;
using Formarca.Application.Common.Models;
using Formarca.Application.Tenancy;
using Formarca.Domain.Tenancy;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Formarca.Application.Tenancy.Queries;

public sealed record ListTenantsQuery(
    int Page = 1,
    int PageSize = Paging.DefaultPageSize,
    IReadOnlyList<TenantStatus>? Statuses = null,
    DateTime? ActiveSince = null,
    string? Search = null) : IRequest<PagedResult<TenantDto>>;

public sealed class ListTenantsQueryHandler(
    IGenericRepository repository,
    ITenantContext tenantContext) : IRequestHandler<ListTenantsQuery, PagedResult<TenantDto>>
{
    public async Task<PagedResult<TenantDto>> Handle(ListTenantsQuery request, CancellationToken cancellationToken)
    {
        tenantContext.SetIgnoreTenantFilters(true);

        var (page, pageSize, skip) = Paging.Normalize(request.Page, request.PageSize);

        var query = repository.Query<Tenant>();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            query = query.Where(tenant =>
                tenant.Name.Contains(term)
                || tenant.Slug.Contains(term)
                || (tenant.Email != null && tenant.Email.Contains(term))
                || (tenant.Document != null && tenant.Document.Contains(term)));
        }

        if (request.Statuses is { Count: > 0 })
        {
            var statuses = request.Statuses.ToList();
            if (request.ActiveSince is DateTime since && statuses.Contains(TenantStatus.Active))
            {
                var open = statuses.Where(status => status != TenantStatus.Active).ToList();
                query = query.Where(tenant =>
                    open.Contains(tenant.Status)
                    || (tenant.Status == TenantStatus.Active && tenant.CreatedAtUtc >= since));
            }
            else
            {
                query = query.Where(tenant => statuses.Contains(tenant.Status));
            }
        }

        query = query.OrderBy(tenant => tenant.Name);
        var total = await query.CountAsync(cancellationToken);
            var items = await query
            .Skip(skip)
            .Take(pageSize)
            .Select(t => new TenantDto(
                t.Id,
                t.Name,
                t.Slug,
                t.Email,
                t.Phone,
                t.Document,
                t.LegalRepresentativeName,
                t.LegalRepresentativeDocument,
                t.QuotedAmount,
                t.BudgetNotes,
                t.Address == null
                    ? null
                    : new AddressDto(
                        t.Address.ZipCode,
                        t.Address.Street,
                        t.Address.Number,
                        t.Address.Complement,
                        t.Address.Neighborhood,
                        t.Address.City,
                        t.Address.State),
                t.Status.ToString(),
                t.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        return new PagedResult<TenantDto>(items, total, page, pageSize);
    }
}

public sealed record GetTenantByIdQuery(Guid Id) : IRequest<TenantDto>;

public sealed class GetTenantByIdQueryHandler(
    IGenericRepository repository,
    ITenantContext tenantContext) : IRequestHandler<GetTenantByIdQuery, TenantDto>
{
    public async Task<TenantDto> Handle(GetTenantByIdQuery request, CancellationToken cancellationToken)
    {
        tenantContext.SetIgnoreTenantFilters(true);

        var tenant = await repository.FirstOrDefaultAsync<Tenant>(t => t.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException("Tenant not found.");

        return tenant.ToTenantDto();
    }
}
