namespace Formarca.Domain.Auditing;

/// <summary>
/// Append-only change record written on SaveChanges. Does not inherit <see cref="Common.Entity"/>
/// so it is not itself audited or soft-deleted.
/// </summary>
public sealed class AuditLog
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid? TenantId { get; set; }

    public Guid? UserId { get; set; }

    public string Action { get; set; } = string.Empty;

    public string EntityName { get; set; } = string.Empty;

    public Guid EntityId { get; set; }

    public DateTime OccurredAtUtc { get; set; }

    public string? JsonChanges { get; set; }
}
