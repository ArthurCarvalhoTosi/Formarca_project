using Formarca.Domain.Tenancy;

namespace Formarca.Application.Common.Models;

public static class TenantMappings
{
    public static TenantDto ToTenantDto(this Tenant tenant) =>
        new(
            tenant.Id,
            tenant.Name,
            tenant.Slug,
            tenant.Email,
            tenant.Phone,
            tenant.Document,
            tenant.LegalRepresentativeName,
            tenant.LegalRepresentativeDocument,
            tenant.QuotedAmount,
            tenant.BudgetNotes,
            tenant.Address?.ToAddressDto(),
            tenant.Status.ToString(),
            tenant.CreatedAtUtc);

    public static RegistrationDto ToRegistrationDto(this Tenant tenant) =>
        new(
            tenant.Id,
            tenant.Name,
            tenant.Slug,
            tenant.Email ?? string.Empty,
            tenant.Phone ?? string.Empty,
            tenant.Document ?? string.Empty,
            tenant.LegalRepresentativeName,
            tenant.LegalRepresentativeDocument,
            tenant.QuotedAmount,
            tenant.BudgetNotes,
            tenant.Address?.ToAddressDto() ?? EmptyAddress,
            tenant.Status.ToString(),
            tenant.CreatedAtUtc);

    public static AddressDto ToAddressDto(this Address address) =>
        new(
            address.ZipCode,
            address.Street,
            address.Number,
            address.Complement,
            address.Neighborhood,
            address.City,
            address.State);

    public static Address ToAddress(this AddressDto dto) =>
        Address.Create(
            dto.ZipCode,
            dto.Street,
            dto.Number,
            dto.Complement,
            dto.Neighborhood,
            dto.City,
            dto.State);

    private static readonly AddressDto EmptyAddress = new("", "", "", null, "", "", "");
}
