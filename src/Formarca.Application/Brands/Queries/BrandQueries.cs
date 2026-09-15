using Formarca.Application.Common;
using Formarca.Application.Common.Exceptions;
using Formarca.Application.Common.Interfaces;
using Formarca.Application.Common.Models;
using Formarca.Domain.Brands;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Formarca.Application.Brands.Queries;

public sealed record ListBrandsQuery(
    Guid? TenantId,
    IReadOnlyList<Guid>? TenantIds,
    string? Name,
    IReadOnlyList<int>? InpiClasses,
    int Page = 1,
    int PageSize = Paging.DefaultPageSize) : IRequest<PagedResult<BrandDto>>;

public sealed class ListBrandsQueryHandler(
    ICurrentUser currentUser,
    IGenericRepository repository,
    ITenantContext tenantContext) : IRequestHandler<ListBrandsQuery, PagedResult<BrandDto>>
{
    public async Task<PagedResult<BrandDto>> Handle(ListBrandsQuery request, CancellationToken cancellationToken)
    {
        CommercialAccess.IgnoreTenantFiltersIfAdmin(tenantContext, currentUser);

        var (page, pageSize, skip) = Paging.Normalize(request.Page, request.PageSize);
        var query = repository.Query<Brand>().Where(b => !b.IsDeleted);

        var tenantIds = request.TenantIds?.Where(id => id != Guid.Empty).Distinct().ToList()
            ?? (request.TenantId is not null && request.TenantId != Guid.Empty
                ? [request.TenantId.Value]
                : new List<Guid>());
        CommercialAccess.EnsureRequestedTenantsAllowed(currentUser, tenantIds);

        if (currentUser.IsPlatformAdmin)
        {
            if (tenantIds.Count > 0)
            {
                query = query.Where(b => tenantIds.Contains(b.TenantId));
            }
        }
        else
        {
            var tenantId = CommercialAccess.ResolveTenantId(
                currentUser,
                tenantIds.Count == 1 ? tenantIds[0] : null,
                requiredForAdmin: false);
            query = query.Where(b => b.TenantId == tenantId);
        }

        if (!string.IsNullOrWhiteSpace(request.Name))
        {
            var name = request.Name.Trim();
            query = query.Where(b => b.Name.Contains(name));
        }

        if (request.InpiClasses is { Count: > 0 })
        {
            var classes = request.InpiClasses.ToList();
            query = query.Where(b => b.InpiClasses.Any(c => classes.Contains(c.Number)));
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(b => b.Name)
            .Skip(skip)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<BrandDto>(
            await CommercialMappings.MapAsync(repository, items, cancellationToken),
            total,
            page,
            pageSize);
    }
}

public sealed record GetBrandByIdQuery(Guid Id) : IRequest<BrandDto>;

public sealed class GetBrandByIdQueryHandler(
    ICurrentUser currentUser,
    IGenericRepository repository,
    ITenantContext tenantContext) : IRequestHandler<GetBrandByIdQuery, BrandDto>
{
    public async Task<BrandDto> Handle(GetBrandByIdQuery request, CancellationToken cancellationToken)
    {
        CommercialAccess.IgnoreTenantFiltersIfAdmin(tenantContext, currentUser);

        var brand = await repository.FirstOrDefaultAsync<Brand>(
            b => b.Id == request.Id && !b.IsDeleted, cancellationToken)
            ?? throw new NotFoundException("Brand not found.");

        var mapped = await CommercialMappings.MapAsync(repository, [brand], cancellationToken);
        return mapped[0];
    }
}
