using Formarca.Application.Common.Exceptions;
using Formarca.Application.Common.Interfaces;
using Formarca.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Formarca.Application.Tenancy.Queries;

public sealed record ListTenantsQuery : IRequest<IReadOnlyList<TenantDto>>;

public sealed class ListTenantsQueryHandler(
    IApplicationDbContext db,
    ITenantContext tenantContext) : IRequestHandler<ListTenantsQuery, IReadOnlyList<TenantDto>>
{
    public async Task<IReadOnlyList<TenantDto>> Handle(ListTenantsQuery request, CancellationToken cancellationToken)
    {
        tenantContext.SetIgnoreTenantFilters(true);

        return await db.Tenants.AsNoTracking()
            .Where(t => !t.IsDeleted)
            .OrderBy(t => t.Name)
            .Select(t => new TenantDto(t.Id, t.Name, t.Slug, t.Document, t.Status.ToString(), t.CreatedAtUtc))
            .ToListAsync(cancellationToken);
    }
}

public sealed record GetTenantByIdQuery(Guid Id) : IRequest<TenantDto>;

public sealed class GetTenantByIdQueryHandler(
    IApplicationDbContext db,
    ITenantContext tenantContext) : IRequestHandler<GetTenantByIdQuery, TenantDto>
{
    public async Task<TenantDto> Handle(GetTenantByIdQuery request, CancellationToken cancellationToken)
    {
        tenantContext.SetIgnoreTenantFilters(true);

        var tenant = await db.Tenants.AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == request.Id && !t.IsDeleted, cancellationToken)
            ?? throw new NotFoundException("Tenant not found.");

        return new TenantDto(tenant.Id, tenant.Name, tenant.Slug, tenant.Document, tenant.Status.ToString(), tenant.CreatedAtUtc);
    }
}
