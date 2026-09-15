using FluentValidation;
using Formarca.Application.Common.Exceptions;
using Formarca.Application.Common.Interfaces;
using Formarca.Application.Common.Models;
using Formarca.Application.IdentityAccess.Auth;
using Formarca.Application.Tenancy;
using Formarca.Domain.Tenancy;
using MediatR;

namespace Formarca.Application.Tenancy.Commands;

public sealed record CreateTenantCommand(string Name, string Slug, string? Document) : IRequest<TenantDto>;

public sealed class CreateTenantCommandValidator : AbstractValidator<CreateTenantCommand>
{
    public CreateTenantCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Slug).NotEmpty().MaximumLength(100)
            .Matches("^[a-z0-9]+(?:-[a-z0-9]+)*$")
            .WithMessage("Slug must be lowercase alphanumeric with hyphens.");
    }
}

public sealed class CreateTenantCommandHandler(
    IGenericRepository repository,
    ITenantContext tenantContext) : IRequestHandler<CreateTenantCommand, TenantDto>
{
    public async Task<TenantDto> Handle(CreateTenantCommand request, CancellationToken cancellationToken)
    {
        tenantContext.SetIgnoreTenantFilters(true);

        var slug = request.Slug.Trim().ToLowerInvariant();
        if (await repository.AnyAsync<Tenant>(t => t.Slug == slug && !t.IsDeleted, cancellationToken))
        {
            throw new ConflictException($"Tenant slug '{slug}' already exists.");
        }

        var tenant = Tenant.Create(request.Name, slug, request.Document);
        repository.Add(tenant);
        await repository.CommitAsync(cancellationToken);

        await AuthTokenIssuer.EnsureDefaultTenantGroupsAsync(repository, tenant.Id, cancellationToken);

        return tenant.ToTenantDto();
    }
}
