using Formarca.Application.Common.Exceptions;
using Formarca.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Formarca.Application.Tenancy.Commands;

public sealed record DeleteTenantCommand(Guid Id) : IRequest;

public sealed class DeleteTenantCommandHandler(
    IApplicationDbContext db,
    ITenantContext tenantContext) : IRequestHandler<DeleteTenantCommand>
{
    public async Task Handle(DeleteTenantCommand request, CancellationToken cancellationToken)
    {
        tenantContext.SetIgnoreTenantFilters(true);

        var tenant = await db.Tenants.FirstOrDefaultAsync(t => t.Id == request.Id && !t.IsDeleted, cancellationToken)
            ?? throw new NotFoundException("Tenant not found.");

        tenant.SoftDelete();
        await db.SaveChangesAsync(cancellationToken);
    }
}
