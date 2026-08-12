namespace Formarca.Domain.Tenancy;

/// <summary>
/// Entity that always belongs to a contracting company (tenant).
/// </summary>
public interface ITenantScoped
{
    Guid TenantId { get; }
}
