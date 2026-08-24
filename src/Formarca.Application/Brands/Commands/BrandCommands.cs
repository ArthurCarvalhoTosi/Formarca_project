using FluentValidation;
using Formarca.Application.Common;
using Formarca.Application.Common.Exceptions;
using Formarca.Application.Common.Interfaces;
using Formarca.Application.Common.Models;
using Formarca.Domain.Brands;
using Formarca.Domain.Contracts;
using Formarca.Domain.Tenancy;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Formarca.Application.Brands.Commands;

public sealed record CreateBrandCommand(
    Guid TenantId,
    string Name,
    string ServicesDescription,
    IReadOnlyList<int>? InpiClasses) : IRequest<BrandDto>;

public sealed class CreateBrandCommandValidator : AbstractValidator<CreateBrandCommand>
{
    public CreateBrandCommandValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.ServicesDescription).NotEmpty().MaximumLength(4000);
        RuleForEach(x => x.InpiClasses).InclusiveBetween(1, 45);
    }
}

public sealed class CreateBrandCommandHandler(
    ICurrentUser currentUser,
    IGenericRepository repository,
    ITenantContext tenantContext) : IRequestHandler<CreateBrandCommand, BrandDto>
{
    public async Task<BrandDto> Handle(CreateBrandCommand request, CancellationToken cancellationToken)
    {
        CommercialAccess.IgnoreTenantFiltersIfAdmin(tenantContext, currentUser);
        var tenantId = CommercialAccess.ResolveTenantId(currentUser, request.TenantId, requiredForAdmin: true);

        var tenantExists = await repository.AnyAsync<Tenant>(
            t => t.Id == tenantId && !t.IsDeleted, cancellationToken);
        if (!tenantExists)
        {
            throw new NotFoundException("Tenant not found.");
        }

        var brand = Brand.Create(tenantId, request.Name, request.ServicesDescription, request.InpiClasses);
        repository.Add(brand);
        await repository.CommitAsync(cancellationToken);
        return brand.ToDto();
    }
}

public sealed record UpdateBrandCommand(
    Guid Id,
    string Name,
    string ServicesDescription,
    IReadOnlyList<int>? InpiClasses) : IRequest<BrandDto>;

public sealed class UpdateBrandCommandValidator : AbstractValidator<UpdateBrandCommand>
{
    public UpdateBrandCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.ServicesDescription).NotEmpty().MaximumLength(4000);
        RuleForEach(x => x.InpiClasses).InclusiveBetween(1, 45);
    }
}

public sealed class UpdateBrandCommandHandler(
    ICurrentUser currentUser,
    IGenericRepository repository,
    ITenantContext tenantContext) : IRequestHandler<UpdateBrandCommand, BrandDto>
{
    public async Task<BrandDto> Handle(UpdateBrandCommand request, CancellationToken cancellationToken)
    {
        CommercialAccess.IgnoreTenantFiltersIfAdmin(tenantContext, currentUser);

        var brand = await repository.FirstOrDefaultAsync<Brand>(
            b => b.Id == request.Id && !b.IsDeleted, cancellationToken)
            ?? throw new NotFoundException("Brand not found.");

        brand.Update(request.Name, request.ServicesDescription, request.InpiClasses);
        await repository.CommitAsync(cancellationToken);
        return brand.ToDto();
    }
}

public sealed record DeleteBrandCommand(Guid Id) : IRequest;

public sealed class DeleteBrandCommandHandler(
    ICurrentUser currentUser,
    IGenericRepository repository,
    ITenantContext tenantContext) : IRequestHandler<DeleteBrandCommand>
{
    public async Task Handle(DeleteBrandCommand request, CancellationToken cancellationToken)
    {
        CommercialAccess.IgnoreTenantFiltersIfAdmin(tenantContext, currentUser);

        var brand = await repository.FirstOrDefaultAsync<Brand>(
            b => b.Id == request.Id && !b.IsDeleted, cancellationToken)
            ?? throw new NotFoundException("Brand not found.");

        var hasContracts = await repository.AnyAsync<Contract>(
            c => c.BrandId == brand.Id && !c.IsDeleted && c.Status != ContractStatus.Cancelled,
            cancellationToken);
        if (hasContracts)
        {
            throw new ConflictException("Brand has contracts.");
        }

        brand.SoftDelete();
        await repository.CommitAsync(cancellationToken);
    }
}
