using FluentValidation;
using Formarca.Application.Common;
using Formarca.Application.Common.Exceptions;
using Formarca.Application.Common.Interfaces;
using Formarca.Application.Common.Models;
using Formarca.Domain.IdentityAccess;
using Formarca.Domain.Invoices;
using Formarca.Domain.Processes;
using Formarca.Domain.Tenancy;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ProcessEntity = Formarca.Domain.Processes.Process;

namespace Formarca.Application.Invoices.Commands;

public sealed record SubmitInvoicePaymentCommand(
    Guid Id,
    PaymentMethod PaymentMethod,
    int Installments) : IRequest<InvoiceDto>;

public sealed class SubmitInvoicePaymentCommandValidator : AbstractValidator<SubmitInvoicePaymentCommand>
{
    public SubmitInvoicePaymentCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.PaymentMethod).IsInEnum();
        RuleFor(x => x.Installments).InclusiveBetween(1, 12);
        RuleFor(x => x.Installments)
            .Equal(1)
            .When(x => x.PaymentMethod == PaymentMethod.Pix)
            .WithMessage("Pix payments must use 1 installment.");
    }
}

public sealed class SubmitInvoicePaymentCommandHandler(
    ICurrentUser currentUser,
    IGenericRepository repository,
    ITenantContext tenantContext) : IRequestHandler<SubmitInvoicePaymentCommand, InvoiceDto>
{
    public async Task<InvoiceDto> Handle(SubmitInvoicePaymentCommand request, CancellationToken cancellationToken)
    {
        CommercialAccess.IgnoreTenantFiltersIfAdmin(tenantContext, currentUser);

        var invoice = await InvoiceCommands.LoadAsync(repository, request.Id, cancellationToken);
        var settings = await InvoiceCommands.LoadSettingsAsync(repository, cancellationToken);

        var expected = InvoicePricing.CalculateExpectedAmount(
            invoice.ContractAmount,
            invoice.Discount,
            settings.GeneralDiscount,
            settings.PixDiscount,
            request.PaymentMethod,
            request.Installments,
            settings.GetInterestRates());

        try
        {
            invoice.SubmitPayment(request.PaymentMethod, request.Installments, expected);
        }
        catch (InvalidOperationException ex)
        {
            throw new ConflictException(ex.Message);
        }

        await repository.CommitAsync(cancellationToken);
        return invoice.ToDto();
    }
}

public sealed record ApproveInvoiceCommand(Guid Id, decimal? ReceivedAmount) : IRequest<InvoiceDto>;

public sealed class ApproveInvoiceCommandValidator : AbstractValidator<ApproveInvoiceCommand>
{
    public ApproveInvoiceCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.ReceivedAmount).GreaterThanOrEqualTo(0).When(x => x.ReceivedAmount.HasValue);
    }
}

public sealed class ApproveInvoiceCommandHandler(
    ICurrentUser currentUser,
    IGenericRepository repository,
    ITenantContext tenantContext) : IRequestHandler<ApproveInvoiceCommand, InvoiceDto>
{
    public async Task<InvoiceDto> Handle(ApproveInvoiceCommand request, CancellationToken cancellationToken)
    {
        CommercialAccess.IgnoreTenantFiltersIfAdmin(tenantContext, currentUser);
        tenantContext.SetIgnoreTenantFilters(true);

        var invoice = await InvoiceCommands.LoadAsync(repository, request.Id, cancellationToken);
        try
        {
            invoice.Approve(request.ReceivedAmount ?? invoice.ExpectedAmount);
        }
        catch (InvalidOperationException ex)
        {
            throw new ConflictException(ex.Message);
        }

        await FunnelActivation.OnInvoicePaidAsync(repository, invoice, cancellationToken);
        await repository.CommitAsync(cancellationToken);
        return invoice.ToDto();
    }
}

public sealed record RejectInvoiceCommand(Guid Id) : IRequest<InvoiceDto>;

public sealed class RejectInvoiceCommandHandler(
    ICurrentUser currentUser,
    IGenericRepository repository,
    ITenantContext tenantContext) : IRequestHandler<RejectInvoiceCommand, InvoiceDto>
{
    public async Task<InvoiceDto> Handle(RejectInvoiceCommand request, CancellationToken cancellationToken)
    {
        CommercialAccess.IgnoreTenantFiltersIfAdmin(tenantContext, currentUser);
        var invoice = await InvoiceCommands.LoadAsync(repository, request.Id, cancellationToken);
        try
        {
            invoice.Reject();
        }
        catch (InvalidOperationException ex)
        {
            throw new ConflictException(ex.Message);
        }

        await repository.CommitAsync(cancellationToken);
        return invoice.ToDto();
    }
}

public sealed record UpdateInvoiceSettingsCommand(
    IReadOnlyDictionary<string, decimal>? InterestRates,
    string? InterestRatesJson,
    decimal PixDiscount,
    decimal GeneralDiscount) : IRequest<InvoiceSettingsDto>;

public sealed class UpdateInvoiceSettingsCommandValidator : AbstractValidator<UpdateInvoiceSettingsCommand>
{
    public UpdateInvoiceSettingsCommandValidator()
    {
        RuleFor(x => x)
            .Must(x => (x.InterestRates is { Count: > 0 }) || !string.IsNullOrWhiteSpace(x.InterestRatesJson))
            .WithMessage("Interest rates are required.");
        RuleFor(x => x.PixDiscount).InclusiveBetween(0, 100);
        RuleFor(x => x.GeneralDiscount).InclusiveBetween(0, 100);
    }
}

public sealed class UpdateInvoiceSettingsCommandHandler(
    IGenericRepository repository,
    ITenantContext tenantContext) : IRequestHandler<UpdateInvoiceSettingsCommand, InvoiceSettingsDto>
{
    public async Task<InvoiceSettingsDto> Handle(UpdateInvoiceSettingsCommand request, CancellationToken cancellationToken)
    {
        tenantContext.SetIgnoreTenantFilters(true);
        var settings = await InvoiceCommands.LoadSettingsAsync(repository, cancellationToken, createIfMissing: true);
        try
        {
            var rates = InvoiceCommands.ResolveInterestRates(request.InterestRates, request.InterestRatesJson);
            settings.Update(rates, request.PixDiscount, request.GeneralDiscount);
        }
        catch (ArgumentException ex)
        {
            throw new ConflictException(ex.Message);
        }

        await repository.CommitAsync(cancellationToken);
        return settings.ToDto();
    }
}

internal static class InvoiceCommands
{
    public static async Task<Invoice> LoadAsync(IGenericRepository repository, Guid id, CancellationToken cancellationToken) =>
        await repository.FirstOrDefaultAsync<Invoice>(i => i.Id == id && !i.IsDeleted, cancellationToken)
            ?? throw new NotFoundException("Invoice not found.");

    public static async Task<InvoiceSettings> LoadSettingsAsync(
        IGenericRepository repository,
        CancellationToken cancellationToken,
        bool createIfMissing = false)
    {
        var settings = await repository.Query<InvoiceSettings>(asNoTracking: false)
            .Where(s => !s.IsDeleted)
            .OrderBy(s => s.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (settings is not null)
        {
            return settings;
        }

        if (!createIfMissing)
        {
            throw new NotFoundException("Invoice settings were not found.");
        }

        settings = InvoiceSettings.CreateDefault();
        repository.Add(settings);
        await repository.CommitAsync(cancellationToken);
        return settings;
    }

    public static InterestRates ResolveInterestRates(
        IReadOnlyDictionary<string, decimal>? interestRates,
        string? interestRatesJson)
    {
        if (interestRates is { Count: > 0 })
        {
            var payload = interestRates.ToDictionary(pair => pair.Key, pair => pair.Value);
            return InterestRates.Parse(System.Text.Json.JsonSerializer.Serialize(payload));
        }

        if (!string.IsNullOrWhiteSpace(interestRatesJson))
        {
            return InterestRates.Parse(interestRatesJson);
        }

        throw new ConflictException("Interest rates are required.");
    }
}

internal static class FunnelActivation
{
    public static async Task OnInvoicePaidAsync(
        IGenericRepository repository,
        Invoice invoice,
        CancellationToken cancellationToken)
    {
        var tenant = await repository.FirstOrDefaultAsync<Tenant>(
            t => t.Id == invoice.TenantId && !t.IsDeleted, cancellationToken)
            ?? throw new NotFoundException("Tenant not found.");

        if (tenant.Status == TenantStatus.Onboarding)
        {
            tenant.ActivateAfterPayment();
            await PromoteOnboardingMembersAsync(repository, tenant.Id, cancellationToken);
        }

        var contract = await repository.FirstOrDefaultAsync<Domain.Contracts.Contract>(
            c => c.Id == invoice.ContractId && !c.IsDeleted, cancellationToken)
            ?? throw new NotFoundException("Contract not found.");

        var processExists = await repository.AnyAsync<ProcessEntity>(
            p => p.ContractId == contract.Id && !p.IsDeleted, cancellationToken);
        if (!processExists)
        {
            repository.Add(ProcessEntity.CreateGenerated(contract.TenantId, contract.BrandId, contract.Id));
        }
    }

    public static async Task PromoteOnboardingMembersAsync(
        IGenericRepository repository,
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        var onboardingGroup = await repository.FirstOrDefaultAsync<Group>(
            g => g.TenantId == tenantId && g.Name == DefaultGroupNames.ClientOnboarding && !g.IsDeleted,
            cancellationToken);
        var clientUsersGroup = await repository.FirstOrDefaultAsync<Group>(
            g => g.TenantId == tenantId && g.Name == DefaultGroupNames.ClientUsers && !g.IsDeleted,
            cancellationToken);

        if (onboardingGroup is null || clientUsersGroup is null)
        {
            return;
        }

        var memberships = await repository.Query<UserGroupMembership>(asNoTracking: false)
            .Where(m => m.TenantId == tenantId && m.GroupId == onboardingGroup.Id && !m.IsDeleted)
            .ToListAsync(cancellationToken);

        foreach (var membership in memberships)
        {
            membership.SoftDelete();

            var alreadyClient = await repository.AnyAsync<UserGroupMembership>(
                m => m.UserId == membership.UserId
                    && m.TenantId == tenantId
                    && m.GroupId == clientUsersGroup.Id
                    && !m.IsDeleted,
                cancellationToken);

            if (!alreadyClient)
            {
                repository.Add(UserGroupMembership.Create(membership.UserId, tenantId, clientUsersGroup.Id));
            }
        }
    }
}
