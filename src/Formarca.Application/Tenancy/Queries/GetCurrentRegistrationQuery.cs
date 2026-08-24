using Formarca.Application.Common.Exceptions;
using Formarca.Application.Common.Interfaces;
using Formarca.Application.Common.Models;
using Formarca.Application.Tenancy;
using Formarca.Domain.Tenancy;
using MediatR;

namespace Formarca.Application.Tenancy.Queries;

public sealed record GetCurrentRegistrationQuery : IRequest<RegistrationDto>;

public sealed class GetCurrentRegistrationQueryHandler(
    ICurrentUser currentUser,
    IGenericRepository repository,
    ITenantContext tenantContext) : IRequestHandler<GetCurrentRegistrationQuery, RegistrationDto>
{
    public async Task<RegistrationDto> Handle(GetCurrentRegistrationQuery request, CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated || currentUser.TenantId is null)
        {
            throw new NotFoundException("No active tenant.");
        }

        tenantContext.SetIgnoreTenantFilters(true);

        var tenant = await repository.FirstOrDefaultAsync<Tenant>(
            t => t.Id == currentUser.TenantId && !t.IsDeleted, cancellationToken)
            ?? throw new NotFoundException("Tenant not found.");

        if (!tenant.Status.AllowsClientAccess() && !currentUser.IsPlatformAdmin)
        {
            throw new ForbiddenException("Tenant is not available.");
        }

        return tenant.ToRegistrationDto();
    }
}

public sealed record GetRegistrationByTenantIdQuery(Guid TenantId) : IRequest<RegistrationDto>;

public sealed class GetRegistrationByTenantIdQueryHandler(
    ICurrentUser currentUser,
    IGenericRepository repository,
    ITenantContext tenantContext) : IRequestHandler<GetRegistrationByTenantIdQuery, RegistrationDto>
{
    public async Task<RegistrationDto> Handle(GetRegistrationByTenantIdQuery request, CancellationToken cancellationToken)
    {
        RegistrationAccess.EnsureCanAccessTenant(currentUser, request.TenantId, missingSessionIsNotFound: true);

        tenantContext.SetIgnoreTenantFilters(true);

        var tenant = await repository.FirstOrDefaultAsync<Tenant>(
            t => t.Id == request.TenantId && !t.IsDeleted, cancellationToken)
            ?? throw new NotFoundException("Tenant not found.");

        if (!tenant.Status.AllowsClientAccess() && !currentUser.IsPlatformAdmin)
        {
            throw new ForbiddenException("Tenant is not available.");
        }

        return tenant.ToRegistrationDto();
    }
}
