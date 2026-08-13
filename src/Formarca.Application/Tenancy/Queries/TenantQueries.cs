using Formarca.Application.Common.Exceptions;
using Formarca.Application.Common.Interfaces;
using Formarca.Application.Common.Models;
using Formarca.Domain.Tenancy;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Formarca.Application.Tenancy.Queries;

public sealed record ListTenantsQuery(int Page = 1, int PageSize = Paging.DefaultPageSize)
    : IRequest<PagedResult<TenantDto>>;

public sealed class ListTenantsQueryHandler(
    IGenericRepository repository,
    ITenantContext tenantContext) : IRequestHandler<ListTenantsQuery, PagedResult<TenantDto>>
{
    public async Task<PagedResult<TenantDto>> Handle(ListTenantsQuery request, CancellationToken cancellationToken)
    {
        tenantContext.SetIgnoreTenantFilters(true);

        var (page, pageSize, skip) = Paging.Normalize(request.Page, request.PageSize);

        var query = repository.Query<Tenant>().OrderBy(t => t.Name);
        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip(skip)
            .Take(pageSize)
            .Select(t => new TenantDto(t.Id, t.Name, t.Slug, t.Document, t.Status.ToString(), t.CreatedAtUtc))
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

        return new TenantDto(tenant.Id, tenant.Name, tenant.Slug, tenant.Document, tenant.Status.ToString(), tenant.CreatedAtUtc);
    }
}
