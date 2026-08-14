namespace Formarca.Application.Common.Models;

public sealed record AuthTokensDto(
    string AccessToken,
    string RefreshToken,
    DateTime AccessTokenExpiresAtUtc,
    Guid? TenantId);

public sealed record TenantSummaryDto(Guid Id, string Name, string Slug, bool IsDefault);

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
    string BrandName,
    string BrandDescription,
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
    string? BrandName,
    string? BrandDescription,
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
