using FluentValidation;
using Formarca.Application.Common.Exceptions;
using Formarca.Application.Common.Interfaces;
using Formarca.Application.Common.Models;
using Formarca.Application.Tenancy;
using Formarca.Domain.Tenancy;
using MediatR;

namespace Formarca.Application.Tenancy.Commands;

public sealed record UpdateTenantCommand(
    Guid Id,
    string Name,
    string? Document,
    TenantStatus Status) : IRequest<TenantDto>;

public sealed class UpdateTenantCommandValidator : AbstractValidator<UpdateTenantCommand>
{
    public UpdateTenantCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Status).IsInEnum();
    }
}

public sealed class UpdateTenantCommandHandler(
    IGenericRepository repository,
    ITenantContext tenantContext) : IRequestHandler<UpdateTenantCommand, TenantDto>
{
    public async Task<TenantDto> Handle(UpdateTenantCommand request, CancellationToken cancellationToken)
    {
        tenantContext.SetIgnoreTenantFilters(true);

        var tenant = await repository.FirstOrDefaultAsync<Tenant>(t => t.Id == request.Id && !t.IsDeleted, cancellationToken)
            ?? throw new NotFoundException("Tenant not found.");

        tenant.Update(request.Name, request.Document, request.Status);
        await repository.CommitAsync(cancellationToken);

        return tenant.ToTenantDto();
    }
}
