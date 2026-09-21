using System.Text.Json.Serialization;

namespace Formarca.Application.Common.Models;

public sealed record AuthTokensDto(
    string AccessToken,
    string RefreshToken,
    DateTime AccessTokenExpiresAtUtc,
    Guid? TenantId);

public sealed record TenantSummaryDto(Guid Id, string Name, string Slug, bool IsDefault, string Status);

public sealed record AuthResultDto(
    AuthTokensDto Tokens,
    Guid UserId,
    string Email,
    string? DisplayName,
    bool IsPlatformAdmin,
    IReadOnlyList<TenantSummaryDto> Tenants,
    IReadOnlyList<string> Permissions);

public sealed record CurrentUserDto(
    Guid UserId,
    string Email,
    string? DisplayName,
    bool IsPlatformAdmin,
    Guid? ActiveTenantId,
    IReadOnlyList<TenantSummaryDto> Tenants,
    IReadOnlyList<string> Permissions);

public sealed record AddressDto(
    string ZipCode,
    string Street,
    string Number,
    string? Complement,
    string Neighborhood,
    string City,
    string State);

public sealed record RegistrationDto(
    Guid Id,
    string Name,
    string Slug,
    string Email,
    string Phone,
    string Document,
    string? LegalRepresentativeName,
    string? LegalRepresentativeDocument,
    decimal? QuotedAmount,
    string? BudgetNotes,
    AddressDto Address,
    string Status,
    DateTime CreatedAtUtc);

public sealed record TenantDto(
    Guid Id,
    string Name,
    string Slug,
    string? Email,
    string? Phone,
    string? Document,
    string? LegalRepresentativeName,
    string? LegalRepresentativeDocument,
    decimal? QuotedAmount,
    string? BudgetNotes,
    AddressDto? Address,
    string Status,
    DateTime CreatedAtUtc);

public sealed record UserDto(
    Guid Id,
    string Email,
    string? DisplayName,
    bool IsPlatformAdmin,
    bool IsActive,
    IReadOnlyList<TenantSummaryDto> Tenants);

public sealed record GroupDto(
    Guid Id,
    Guid? TenantId,
    string Name,
    string? Description,
    IReadOnlyList<string> PermissionCodes);

public sealed record PermissionDto(Guid Id, string Code, string Name);

public sealed record BrandDto(
    Guid Id,
    Guid TenantId,
    string Name,
    string ServicesDescription,
    IReadOnlyList<int> InpiClasses,
    DateTime CreatedAtUtc,
    string? TenantName = null);

public sealed record ContractDto(
    Guid Id,
    Guid TenantId,
    Guid BrandId,
    string Code,
    string CompanyDocument,
    string RepresentativeDocument,
    string BrandName,
    string Classes,
    decimal Amount,
    string Status,
    DateTime FormarcaSignedAt,
    DateTime? ClientSignedAt,
    DateTime GeneratedAtUtc,
    DateTime? SignedAtUtc,
    DateTime CreatedAtUtc,
    string TemplateKey,
    string BodyMarkdown,
    string CompanyName,
    string Street,
    string Number,
    string City,
    string State,
    string ZipCode,
    string MinimumTerm,
    byte[]? ClientSignaturePng = null,
    string? TenantName = null,
    decimal? PaidAmount = null,
    string? PaymentMethod = null,
    Guid? InvoiceId = null,
    string? AutentiqueDocumentId = null,
    string? AutentiqueSignUrl = null,
    string? AutentiqueStatus = null);

public sealed record ContractSignLinkDto(
    [property: JsonPropertyName("signUrl")] string SignUrl);

public sealed record InvoiceDto(
    Guid Id,
    Guid TenantId,
    Guid ContractId,
    string? PaymentMethod,
    int? Installments,
    decimal ContractAmount,
    decimal Discount,
    decimal ExpectedAmount,
    decimal ReceivedAmount,
    string Status,
    DateTime CreatedAtUtc,
    string? TenantName = null,
    string? ContractCode = null);

public sealed record InvoiceSettingsDto(
    Guid Id,
    IReadOnlyDictionary<string, decimal> InterestRates,
    string InterestRatesJson,
    decimal PixDiscount,
    decimal GeneralDiscount);

public sealed record ProcessDto(
    Guid Id,
    Guid TenantId,
    Guid BrandId,
    Guid ContractId,
    string Status,
    string? ProcessNumber,
    DateTime CreatedAtUtc,
    string? BrandName = null,
    string? ContractCode = null,
    Guid? TrademarkStatusId = null,
    string? TrademarkStatusTitle = null,
    string? TrademarkStatusTone = null,
    DateTime? TrademarkStatusChangedAtUtc = null);

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ProcessFunnelStage
{
    Brand,
    Contract,
    Invoice,
    Process
}

public sealed record ProcessDashboardContractDto(
    Guid Id,
    string Code,
    string Status);

public sealed record ProcessDashboardInvoiceDto(
    Guid Id,
    string Status,
    decimal ExpectedAmount);

public sealed record ProcessDashboardProcessDto(
    Guid Id,
    string Status,
    string? ProcessNumber);

public sealed record ProcessDashboardItemDto(
    Guid BrandId,
    string BrandName,
    Guid TenantId,
    string? TenantName,
    ProcessDashboardContractDto? Contract,
    ProcessDashboardInvoiceDto? Invoice,
    ProcessDashboardProcessDto? Process,
    ProcessFunnelStage FunnelStage);

public sealed record ProcessDashboardKpisDto(
    int Brands,
    int ContractsInProgress,
    int InvoicesPending,
    int ProcessesGenerated);

public sealed record ProcessDashboardResult(
    IReadOnlyList<ProcessDashboardItemDto> Items,
    ProcessDashboardKpisDto Kpis,
    int TotalCount,
    int Page,
    int PageSize);

public sealed record ContractDocumentFile(string FileName, string ContentType, byte[] Content);

public sealed record UserTableViewDto(
    Guid Id,
    string TableKey,
    string Name,
    IReadOnlyList<string> ColumnKeys,
    bool IsActive,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);
