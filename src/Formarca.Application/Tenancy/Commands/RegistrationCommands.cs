using FluentValidation;
using Formarca.Application.Common.Exceptions;
using Formarca.Application.Common.Interfaces;
using Formarca.Application.Common.Models;
using Formarca.Application.Tenancy;
using Formarca.Domain.Tenancy;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Formarca.Application.Tenancy.Commands;

public sealed record SubmitRegistrationCommand(
    string Name,
    string Email,
    string Phone,
    string Document,
    AddressDto Address,
    string BrandName,
    string BrandDescription) : IRequest<RegistrationDto>, IRegistrationFields;

public sealed class SubmitRegistrationCommandValidator : AbstractValidator<SubmitRegistrationCommand>
{
    public SubmitRegistrationCommandValidator()
    {
        RegistrationValidationRules.Apply(this);
    }
}

public sealed class SubmitRegistrationCommandHandler(
    IGenericRepository repository,
    IIdentityService identity,
    ITenantContext tenantContext) : IRequestHandler<SubmitRegistrationCommand, RegistrationDto>
{
    public async Task<RegistrationDto> Handle(SubmitRegistrationCommand request, CancellationToken cancellationToken)
    {
        tenantContext.SetIgnoreTenantFilters(true);

        await RegistrationUniqueness.EnsureAvailableAsync(
            repository, identity, request.Email, excludeTenantId: null, currentEmail: null, cancellationToken);

        var slug = await RegistrationUniqueness.AllocateSlugAsync(repository, request.Name, cancellationToken);
        var tenant = Tenant.Register(
            request.Name,
            slug,
            request.Email,
            request.Phone,
            request.Document,
            request.Address.ToAddress(),
            request.BrandName,
            request.BrandDescription);

        repository.Add(tenant);
        await repository.CommitAsync(cancellationToken);

        return tenant.ToRegistrationDto();
    }
}

public sealed record UpdateRegistrationCommand(
    string Name,
    string Email,
    string Phone,
    string Document,
    AddressDto Address,
    string BrandName,
    string BrandDescription) : IRequest<RegistrationDto>, IRegistrationFields;

public sealed class UpdateRegistrationCommandValidator : AbstractValidator<UpdateRegistrationCommand>
{
    public UpdateRegistrationCommandValidator()
    {
        RegistrationValidationRules.Apply(this);
    }
}

public sealed class UpdateRegistrationCommandHandler(
    ICurrentUser currentUser,
    IGenericRepository repository,
    IIdentityService identity,
    ITenantContext tenantContext) : IRequestHandler<UpdateRegistrationCommand, RegistrationDto>
{
    public async Task<RegistrationDto> Handle(UpdateRegistrationCommand request, CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated || currentUser.TenantId is null)
        {
            throw new ForbiddenException("No active tenant.");
        }

        tenantContext.SetIgnoreTenantFilters(true);

        var tenant = await repository.FirstOrDefaultAsync<Tenant>(
            t => t.Id == currentUser.TenantId && !t.IsDeleted, cancellationToken)
            ?? throw new NotFoundException("Tenant not found.");

        if (tenant.Status != TenantStatus.Active && !currentUser.IsPlatformAdmin)
        {
            throw new ForbiddenException("Tenant is not available.");
        }

        await RegistrationUniqueness.EnsureAvailableAsync(
            repository, identity, request.Email, tenant.Id, tenant.Email, cancellationToken);

        tenant.UpdateRegistration(
            request.Name,
            request.Email,
            request.Phone,
            request.Document,
            request.Address.ToAddress(),
            request.BrandName,
            request.BrandDescription);

        await repository.CommitAsync(cancellationToken);
        return tenant.ToRegistrationDto();
    }
}

public sealed record UpdateRegistrationByTenantIdCommand(
    Guid TenantId,
    string Name,
    string Email,
    string Phone,
    string Document,
    AddressDto Address,
    string BrandName,
    string BrandDescription) : IRequest<RegistrationDto>, IRegistrationFields;

public sealed class UpdateRegistrationByTenantIdCommandValidator : AbstractValidator<UpdateRegistrationByTenantIdCommand>
{
    public UpdateRegistrationByTenantIdCommandValidator()
    {
        RegistrationValidationRules.Apply(this);
    }
}

public sealed class UpdateRegistrationByTenantIdCommandHandler(
    ICurrentUser currentUser,
    IGenericRepository repository,
    IIdentityService identity,
    ITenantContext tenantContext) : IRequestHandler<UpdateRegistrationByTenantIdCommand, RegistrationDto>
{
    public async Task<RegistrationDto> Handle(UpdateRegistrationByTenantIdCommand request, CancellationToken cancellationToken)
    {
        RegistrationAccess.EnsureCanAccessTenant(
            currentUser, request.TenantId, missingSessionIsNotFound: false);

        tenantContext.SetIgnoreTenantFilters(true);

        var tenant = await repository.FirstOrDefaultAsync<Tenant>(
            t => t.Id == request.TenantId && !t.IsDeleted, cancellationToken)
            ?? throw new NotFoundException("Tenant not found.");

        if (tenant.Status != TenantStatus.Active && !currentUser.IsPlatformAdmin)
        {
            throw new ForbiddenException("Tenant is not available.");
        }

        await RegistrationUniqueness.EnsureAvailableAsync(
            repository, identity, request.Email, tenant.Id, tenant.Email, cancellationToken);

        tenant.UpdateRegistration(
            request.Name,
            request.Email,
            request.Phone,
            request.Document,
            request.Address.ToAddress(),
            request.BrandName,
            request.BrandDescription);

        await repository.CommitAsync(cancellationToken);
        return tenant.ToRegistrationDto();
    }
}

internal static class RegistrationUniqueness
{
    public static async Task EnsureAvailableAsync(
        IGenericRepository repository,
        IIdentityService identity,
        string email,
        Guid? excludeTenantId,
        string? currentEmail,
        CancellationToken cancellationToken)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();

        var emailTaken = await repository.Query<Tenant>()
            .AnyAsync(
                t => !t.IsDeleted
                    && t.Email != null
                    && t.Email.ToLower() == normalizedEmail
                    && (excludeTenantId == null || t.Id != excludeTenantId),
                cancellationToken);

        if (emailTaken)
        {
            throw new ConflictException("A tenant with this email already exists.");
        }

        var emailChanged = string.IsNullOrWhiteSpace(currentEmail)
            || !string.Equals(currentEmail.Trim(), normalizedEmail, StringComparison.OrdinalIgnoreCase);

        if (emailChanged)
        {
            var existingUser = await identity.FindByEmailAsync(normalizedEmail, cancellationToken);
            if (existingUser is not null)
            {
                throw new ConflictException("A user with this email already exists.");
            }
        }
    }

    public static async Task<string> AllocateSlugAsync(
        IGenericRepository repository,
        string name,
        CancellationToken cancellationToken)
    {
        var baseSlug = TenantSlug.FromName(name);
        var slug = baseSlug;
        var suffix = 2;

        while (await repository.AnyAsync<Tenant>(t => t.Slug == slug && !t.IsDeleted, cancellationToken))
        {
            var suffixText = $"-{suffix}";
            var maxBaseLength = Math.Max(1, 100 - suffixText.Length);
            var trimmed = baseSlug.Length <= maxBaseLength ? baseSlug : baseSlug[..maxBaseLength].TrimEnd('-');
            slug = trimmed + suffixText;
            suffix++;
        }

        return slug;
    }
}
