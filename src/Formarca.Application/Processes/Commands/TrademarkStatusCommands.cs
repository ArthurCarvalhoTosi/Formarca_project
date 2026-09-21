using FluentValidation;
using Formarca.Application.Common.Exceptions;
using Formarca.Application.Common.Interfaces;
using Formarca.Domain.Processes;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Formarca.Application.Processes.Commands;

public sealed record CreateTrademarkStatusCommand(
    string? Code,
    string Title,
    string Description,
    TrademarkStatusPhase Phase,
    TrademarkStatusTone Tone,
    int? SortOrder,
    bool IsActive = true) : IRequest<TrademarkStatusDto>;

public sealed class CreateTrademarkStatusCommandValidator : AbstractValidator<CreateTrademarkStatusCommand>
{
    public CreateTrademarkStatusCommandValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(TrademarkStatus.TitleMaxLength);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(TrademarkStatus.DescriptionMaxLength);
        RuleFor(x => x.Code).MaximumLength(TrademarkStatus.CodeMaxLength);
        RuleFor(x => x.Phase).IsInEnum();
        RuleFor(x => x.Tone).IsInEnum();
        RuleFor(x => x.SortOrder).GreaterThanOrEqualTo(0).When(x => x.SortOrder.HasValue);
    }
}

public sealed class CreateTrademarkStatusCommandHandler(
    ICurrentUser currentUser,
    IGenericRepository repository) : IRequestHandler<CreateTrademarkStatusCommand, TrademarkStatusDto>
{
    public async Task<TrademarkStatusDto> Handle(
        CreateTrademarkStatusCommand request,
        CancellationToken cancellationToken)
    {
        TrademarkWorkflow.EnsurePlatformAdmin(currentUser);

        var code = string.IsNullOrWhiteSpace(request.Code)
            ? TrademarkStatus.NormalizeCode(request.Title)
            : TrademarkStatus.NormalizeCode(request.Code);
        code = await EnsureUniqueCodeAsync(repository, code, cancellationToken);

        var sortOrder = request.SortOrder ?? await NextSortOrderAsync(repository, cancellationToken);
        TrademarkStatus status;
        try
        {
            status = TrademarkStatus.Create(
                code,
                request.Title,
                request.Description,
                sortOrder,
                request.Phase,
                request.Tone,
                request.IsActive);
        }
        catch (ArgumentException ex)
        {
            throw new ConflictException(ex.Message);
        }

        repository.Add(status);
        await repository.CommitAsync(cancellationToken);
        return status.ToDto();
    }

    private static async Task<string> EnsureUniqueCodeAsync(
        IGenericRepository repository,
        string code,
        CancellationToken cancellationToken)
    {
        var candidate = code;
        var suffix = 2;
        while (await repository.AnyAsync<TrademarkStatus>(
            status => status.Code == candidate && !status.IsDeleted,
            cancellationToken))
        {
            var tail = "-" + suffix;
            var head = code.Length + tail.Length > TrademarkStatus.CodeMaxLength
                ? code[..(TrademarkStatus.CodeMaxLength - tail.Length)].Trim('-')
                : code;
            candidate = head + tail;
            suffix++;
        }

        return candidate;
    }

    private static async Task<int> NextSortOrderAsync(
        IGenericRepository repository,
        CancellationToken cancellationToken)
    {
        var max = await repository.Query<TrademarkStatus>()
            .Where(status => !status.IsDeleted)
            .Select(status => (int?)status.SortOrder)
            .MaxAsync(cancellationToken);
        return (max ?? 0) + 10;
    }
}

public sealed record UpdateTrademarkStatusCommand(
    Guid Id,
    string Title,
    string Description,
    int SortOrder,
    TrademarkStatusPhase Phase,
    TrademarkStatusTone Tone,
    bool IsActive) : IRequest<TrademarkStatusDto>;

public sealed class UpdateTrademarkStatusCommandValidator : AbstractValidator<UpdateTrademarkStatusCommand>
{
    public UpdateTrademarkStatusCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(TrademarkStatus.TitleMaxLength);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(TrademarkStatus.DescriptionMaxLength);
        RuleFor(x => x.SortOrder).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Phase).IsInEnum();
        RuleFor(x => x.Tone).IsInEnum();
    }
}

public sealed class UpdateTrademarkStatusCommandHandler(
    ICurrentUser currentUser,
    IGenericRepository repository) : IRequestHandler<UpdateTrademarkStatusCommand, TrademarkStatusDto>
{
    public async Task<TrademarkStatusDto> Handle(
        UpdateTrademarkStatusCommand request,
        CancellationToken cancellationToken)
    {
        TrademarkWorkflow.EnsurePlatformAdmin(currentUser);
        var status = await TrademarkWorkflow.LoadStatusAsync(repository, request.Id, cancellationToken);
        try
        {
            status.Update(
                request.Title,
                request.Description,
                request.SortOrder,
                request.Phase,
                request.Tone,
                request.IsActive);
        }
        catch (ArgumentException ex)
        {
            throw new ConflictException(ex.Message);
        }

        await repository.CommitAsync(cancellationToken);
        return status.ToDto();
    }
}

public sealed record DeleteTrademarkStatusCommand(Guid Id) : IRequest;

public sealed class DeleteTrademarkStatusCommandValidator : AbstractValidator<DeleteTrademarkStatusCommand>
{
    public DeleteTrademarkStatusCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public sealed class DeleteTrademarkStatusCommandHandler(
    ICurrentUser currentUser,
    IGenericRepository repository) : IRequestHandler<DeleteTrademarkStatusCommand>
{
    public async Task Handle(DeleteTrademarkStatusCommand request, CancellationToken cancellationToken)
    {
        TrademarkWorkflow.EnsurePlatformAdmin(currentUser);
        var status = await TrademarkWorkflow.LoadStatusAsync(repository, request.Id, cancellationToken);

        var usedByProcess = await repository.AnyAsync<Domain.Processes.Process>(
            process => process.TrademarkStatusId == status.Id && !process.IsDeleted,
            cancellationToken);
        var usedByHistory = await repository.AnyAsync<ProcessStatusChange>(
            change => (change.ToStatusId == status.Id || change.FromStatusId == status.Id) && !change.IsDeleted,
            cancellationToken);
        if (usedByProcess || usedByHistory)
        {
            throw new ConflictException("Status is in use and cannot be deleted. Deactivate it instead.");
        }

        status.SoftDelete();
        await repository.CommitAsync(cancellationToken);
    }
}

public sealed record ReorderTrademarkStatusesCommand(IReadOnlyList<Guid> OrderedIds) : IRequest<IReadOnlyList<TrademarkStatusDto>>;

public sealed class ReorderTrademarkStatusesCommandValidator : AbstractValidator<ReorderTrademarkStatusesCommand>
{
    public ReorderTrademarkStatusesCommandValidator()
    {
        RuleFor(x => x.OrderedIds).NotEmpty();
        RuleForEach(x => x.OrderedIds).NotEmpty();
    }
}

public sealed class ReorderTrademarkStatusesCommandHandler(
    ICurrentUser currentUser,
    IGenericRepository repository) : IRequestHandler<ReorderTrademarkStatusesCommand, IReadOnlyList<TrademarkStatusDto>>
{
    public async Task<IReadOnlyList<TrademarkStatusDto>> Handle(
        ReorderTrademarkStatusesCommand request,
        CancellationToken cancellationToken)
    {
        TrademarkWorkflow.EnsurePlatformAdmin(currentUser);
        var statuses = await repository.Query<TrademarkStatus>()
            .Where(status => !status.IsDeleted)
            .ToListAsync(cancellationToken);

        if (request.OrderedIds.Distinct().Count() != request.OrderedIds.Count
            || request.OrderedIds.Count != statuses.Count
            || statuses.Select(status => status.Id).Except(request.OrderedIds).Any())
        {
            throw new ConflictException("Reorder must include every status once.");
        }

        var byId = statuses.ToDictionary(status => status.Id);
        for (var index = 0; index < request.OrderedIds.Count; index++)
        {
            byId[request.OrderedIds[index]].SetSortOrder((index + 1) * 10);
        }

        await repository.CommitAsync(cancellationToken);
        return statuses
            .OrderBy(status => status.SortOrder)
            .ThenBy(status => status.Title)
            .Select(status => status.ToDto())
            .ToList();
    }
}
