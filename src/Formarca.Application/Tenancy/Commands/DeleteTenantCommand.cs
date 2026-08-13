using Formarca.Application.Common.Exceptions;
using Formarca.Application.Common.Interfaces;
using Formarca.Domain.Tenancy;
using MediatR;

namespace Formarca.Application.Tenancy.Commands;

public sealed record DeleteTenantCommand(Guid Id) : IRequest;

public sealed class DeleteTenantCommandHandler(
    IGenericRepository repository,
    ITenantContext tenantContext) : IRequestHandler<DeleteTenantCommand>
{
    public async Task Handle(DeleteTenantCommand request, CancellationToken cancellationToken)
    {
        tenantContext.SetIgnoreTenantFilters(true);

        var tenant = await repository.FirstOrDefaultAsync<Tenant>(t => t.Id == request.Id && !t.IsDeleted, cancellationToken)
            ?? throw new NotFoundException("Tenant not found.");

        tenant.SoftDelete();
        await repository.CommitAsync(cancellationToken);
    }
}
