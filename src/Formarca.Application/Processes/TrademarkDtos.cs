using Formarca.Domain.Processes;

namespace Formarca.Application.Processes;

public sealed record TrademarkStatusDto(
    Guid Id,
    string Code,
    string Title,
    string Description,
    int SortOrder,
    TrademarkStatusPhase Phase,
    TrademarkStatusTone Tone,
    bool IsActive,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);

public sealed record ProcessStatusChangeDto(
    Guid Id,
    Guid? FromStatusId,
    string? FromStatusTitle,
    Guid ToStatusId,
    string ToStatusTitle,
    string? Note,
    Guid? ChangedByUserId,
    DateTime OccurredAtUtc);

public sealed record TrademarkDashboardItemDto(
    Guid ProcessId,
    Guid BrandId,
    string BrandName,
    Guid TenantId,
    string? TenantName,
    Guid ContractId,
    string? ContractCode,
    string? ProcessNumber,
    DateTime CreatedAtUtc,
    TrademarkStatusDto? Status,
    DateTime? StatusChangedAtUtc,
    string? LatestNote);

public sealed record TrademarkPhaseCountDto(
    TrademarkStatusPhase Phase,
    int Count);

public sealed record TrademarkDashboardKpisDto(
    int Total,
    int WithoutStatus,
    int ActionRequired,
    int Paused,
    int ActiveRights,
    int Closed,
    IReadOnlyList<TrademarkPhaseCountDto> Phases);

public sealed record TrademarkDashboardResult(
    IReadOnlyList<TrademarkDashboardItemDto> Items,
    TrademarkDashboardKpisDto Kpis,
    int TotalCount,
    int Page,
    int PageSize);

public sealed record ProcessTrademarkDetailDto(
    Guid ProcessId,
    Guid BrandId,
    string BrandName,
    Guid TenantId,
    string? TenantName,
    Guid ContractId,
    string? ContractCode,
    string? ProcessNumber,
    TrademarkStatusDto? Status,
    DateTime? StatusChangedAtUtc,
    IReadOnlyList<ProcessStatusChangeDto> History);
