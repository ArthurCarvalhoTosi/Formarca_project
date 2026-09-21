using FluentValidation;
using Formarca.Application.Common;
using Formarca.Application.Common.Exceptions;
using Formarca.Application.Common.Interfaces;
using Formarca.Domain.Processes;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ProcessEntity = Formarca.Domain.Processes.Process;

namespace Formarca.Application.Processes.Commands;

public sealed record ChangeProcessTrademarkStatusCommand(
    Guid ProcessId,
    Guid TrademarkStatusId,
    string? Note,
    string? ProcessNumber) : IRequest<ProcessTrademarkDetailDto>;

public sealed class ChangeProcessTrademarkStatusCommandValidator : AbstractValidator<ChangeProcessTrademarkStatusCommand>
{
    public ChangeProcessTrademarkStatusCommandValidator()
    {
        RuleFor(x => x.ProcessId).NotEmpty();
        RuleFor(x => x.TrademarkStatusId).NotEmpty();
        RuleFor(x => x.Note).MaximumLength(ProcessStatusChange.NoteMaxLength);
        RuleFor(x => x.ProcessNumber).MaximumLength(64);
    }
}

public sealed class ChangeProcessTrademarkStatusCommandHandler(
    ICurrentUser currentUser,
    IGenericRepository repository,
    ITenantContext tenantContext) : IRequestHandler<ChangeProcessTrademarkStatusCommand, ProcessTrademarkDetailDto>
{
    public async Task<ProcessTrademarkDetailDto> Handle(
        ChangeProcessTrademarkStatusCommand request,
        CancellationToken cancellationToken)
    {
        CommercialAccess.IgnoreTenantFiltersIfAdmin(tenantContext, currentUser);

        var process = await repository.FirstOrDefaultAsync<ProcessEntity>(
            item => item.Id == request.ProcessId && !item.IsDeleted,
            cancellationToken)
            ?? throw new NotFoundException("Process not found.");

        CommercialAccess.EnsureRequestedTenantsAllowed(currentUser, [process.TenantId]);

        var status = await TrademarkWorkflow.LoadStatusAsync(repository, request.TrademarkStatusId, cancellationToken);
        if (!status.IsActive)
        {
            throw new ConflictException("Inactive statuses cannot be assigned.");
        }

        var statusChanged = process.TrademarkStatusId != status.Id;
        var nextNumber = request.ProcessNumber is null
            ? process.ProcessNumber
            : string.IsNullOrWhiteSpace(request.ProcessNumber) ? null : request.ProcessNumber.Trim();
        var numberChanged = request.ProcessNumber is not null && nextNumber != process.ProcessNumber;
        if (!statusChanged && !numberChanged)
        {
            throw new ConflictException("Process already has this status.");
        }

        if (statusChanged)
        {
            var fromStatusId = process.TrademarkStatusId;
            try
            {
                process.AssignTrademarkStatus(status.Id);
                repository.Add(ProcessStatusChange.Record(
                    process.TenantId,
                    process.Id,
                    fromStatusId,
                    status.Id,
                    currentUser.UserId,
                    request.Note));
            }
            catch (ArgumentException ex)
            {
                throw new ConflictException(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                throw new ConflictException(ex.Message);
            }
        }

        if (request.ProcessNumber is not null)
        {
            try
            {
                process.SetProcessNumber(request.ProcessNumber);
            }
            catch (ArgumentException ex)
            {
                throw new ConflictException(ex.Message);
            }
        }

        await repository.CommitAsync(cancellationToken);
        return await TrademarkWorkflow.BuildDetailAsync(repository, process, cancellationToken);
    }
}
